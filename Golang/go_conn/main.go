package main

import (
	"bufio"
	"encoding/json"
	"fmt"
	"runtime"
	"strings"

	"go_raft/raft"

	"net/rpc"
	"os"
	"os/signal"
	"sync"
	"syscall"
	"time"

	"github.com/Microsoft/go-winio"
	log "github.com/sirupsen/logrus"
)

// Types required to pass actions
const (
	None    int = 0
	Take    int = 1
	Shoot   int = 2
	Melee   int = 3
	Special int = 4
	Super   int = 5
	Damage  int = 6
	Spawn   int = 7
	Die     int = 8
)

type PlayerID string
type Vector3 struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
	Z float64 `json:"z"`
}
type Quaternion struct {
	W float64 `json:"w"`
	X float64 `json:"x"`
	Y float64 `json:"y"`
	Z float64 `json:"z"`
}
type ActionImpl struct {
	Position Vector3    `json:"Position"`
	Rotation Quaternion `json:"Rotation"`
	Type     int        `json:"Type"`
	Arg      string     `json:"Arg"`
}
type GameLog struct {
	Id       PlayerID   `json:"Id"`
	ActionId int64      `json:"ActionId"`
	Type     string     `json:"Type"`
	Action   ActionImpl `json:"Action"`
}

var actionId int = 0

// Option struct to centralize all channels
type options struct {
	actionChan             chan GameLog
	configurationChan      chan bool
	firstLeader            raft.ServerID
	connections            *sync.Map
	otherServers           []raft.ServerID
	id                     raft.ServerID
	connectedChan          chan bool
	disconnectedChan       chan bool
	requestConnectionChan  chan raft.RequestConnection
	requestNewServerIDChan chan bool
	getNewServerIDChan     chan raft.ServerID
	removedFromGameChan    chan bool
}

// Get time of game
func getNowMs() int64 {
	return time.Now().UnixNano() / int64(time.Millisecond)
}

// Add to known servers - If id of argument is already in "otherServers" dont bother, else just add to "otherServers"
func addToKnownServers(opt *options, id raft.ServerID) {
	var shouldAdd = true
	for _, val := range (*opt).otherServers {
		if val == id {
			shouldAdd = false
			break
		}
	}
	if shouldAdd {
		(*opt).otherServers = append((*opt).otherServers, id)
	}
}

// If there is a request for a new serverID, pass the next server in array "otherServers", in a circular manner, else send self
func connectionPool(opt *options) {
	var currentServer = -1
	for {
		<-(*opt).requestNewServerIDChan
		if len((*opt).otherServers) > 0 {
			currentServer = (currentServer + 1) % len((*opt).otherServers)
			(*opt).getNewServerIDChan <- (*opt).otherServers[currentServer]
		} else {
			(*opt).getNewServerIDChan <- (*opt).id
		}
	}
}

// See if action (GameLog), represented by rpc.Call, was correctly applied and put in bool channel "actionDone", keeping track of timeout (more than one second to apply)
func handleActionResponse(call *rpc.Call, response *raft.ActionResponse, changeConnectionChan chan raft.ServerID, msg GameLog, timestamp int64, currentConnection raft.ServerID, opt *options, actionDoneChannel chan bool) {
	var waitTime time.Duration = 1000
	select {
	//Call done (not timeout)
	case <-call.Done:
		//Case wasnt applied
		if !(*response).Applied {
			log.Trace("Main - Action not applied ", currentConnection, " - ", (*response))
			//Case there is a leader of raft, addToServers and changeConnection to host him
			if (*response).LeaderID != "" {
				addToKnownServers(opt, (*response).LeaderID)
				changeConnectionChan <- (*response).LeaderID
			} else {
				//Request connection to raft
				//Modified
				(*opt).requestConnectionChan <- raft.RequestConnection{Id: currentConnection, Destination: (*opt).connections}
				//eModified
				//Send self, instead of leader
				changeConnectionChan <- (*opt).id
			}
			actionDoneChannel <- false
		} else {
			actionDoneChannel <- true
		}
	//Timeout
	case <-time.After(time.Millisecond * waitTime):
		changeConnectionChan <- ""
		actionDoneChannel <- false
	}
}

// See if configuration, represented by rpc.Call, was succeful (raft.AddRemoveServerResponse.Sucess), keeping track of timeout
func handleConfigurationResponse(call *rpc.Call, response *raft.AddRemoveServerResponse, changeConnectionChan chan raft.ServerID, msg bool, currentConnection raft.ServerID, opt *options) {
	var waitTime time.Duration = 5000
	//If there is no call (configuring self), send to channel and stop routine.
	if call == nil {
		time.Sleep(time.Millisecond * 300)
		(*opt).configurationChan <- msg
		return
	}

	select {
	//Call done (not timeout)
	case <-call.Done:

		//"msg" represents if caller is connecting or disconnecting

		//So if response is not succeful (representing configuration is not applied)
		//Does the same as handleActionResponse (dispose and try reconnect)
		if !(*response).Success {
			log.Trace("Main - Connection not applied ", currentConnection, " - ", (*response))
			if (*response).LeaderID != "" {
				addToKnownServers(opt, (*response).LeaderID)
				changeConnectionChan <- (*response).LeaderID
			} else {
				//Modified
				(*opt).requestConnectionChan <- raft.RequestConnection{Id: currentConnection, Destination: (*opt).connections}
				//eModified
				changeConnectionChan <- (*opt).id
			}
			time.Sleep(time.Millisecond * 300)
			// Send again
			(*opt).configurationChan <- msg
		} else { //Condition "msg" to connect or disconnect
			if msg {
				(*opt).connectedChan <- true
			} else {
				(*opt).disconnectedChan <- true
			}
		}
	//If timeout, send msg to channel and nullify the connection changed (by sending empty serverID)
	case <-time.After(time.Millisecond * waitTime):
		changeConnectionChan <- ""
		(*opt).configurationChan <- msg
	}
}

// Manage all types of actions coming from UI - Engine, etc
func manageActions(opt *options) {
	currentConnection := (*opt).firstLeader
	changeConnectionChan := make(chan raft.ServerID)
	var actions []GameLog
	var clearToSend = true
	actionDoneChannel := make(chan bool)
	for {
		select {
		//If new action comes by the channel, append before switch
		case msg := <-(*opt).actionChan: //actionChan is UI.go originated
			if len(actions) < 32 {
				actions = append(actions, msg)
			} else { //There is a limit of actions enqueued (32)
				log.Debug("Too many actions, drop one")
			}
		//Same for configuration comming through that channel
		case msg := <-(*opt).configurationChan:
			//Response of configuration, usually containing the raft leader ID and success/completion of connect
			var configurationResponse raft.AddRemoveServerResponse
			//Modified
			//Args define who wants to connect/disconnect and that bool (msg = connect=True or disconnect=False)
			var configurationArgs = raft.AddRemoveServerArgs{Server: (*opt).id, Add: msg}
			//eModified
			//Load connection in the map (if it exists)
			var conn, found = (*(*opt).connections).Load(currentConnection)
			if !found {
				//Modified
				//Create a request for connection (Register)
				(*opt).requestConnectionChan <- raft.RequestConnection{Id: currentConnection, Destination: (*opt).connections}
				//eModified
				//Thread the handle, but without the call, as there is none connection to that client/leader.
				go handleConfigurationResponse(nil, nil, changeConnectionChan, msg, currentConnection, opt)
				//Get new ID for self.
				(*opt).requestNewServerIDChan <- true
				currentConnection = <-(*opt).getNewServerIDChan
			} else {
				//As there is already a connection in map.
				log.Info("Main - Send connection request to ", currentConnection)
				var raftConn = conn.(raft.RaftConnection)
				//Call disconnect in other client (the connection hosts)
				actionCall := raftConn.Connection.Go("RaftListener.AddRemoveServerRPC", &configurationArgs, &configurationResponse, nil)
				//Handle de call to disconnect.
				go handleConfigurationResponse(actionCall, &configurationResponse, changeConnectionChan, msg, currentConnection, opt)
			}
		//handleConfigurationResponse calls this channel with the ID of server
		case newServerID := <-changeConnectionChan:
			//Case is not filled, asks in Raft
			if newServerID == "" {
				(*opt).requestNewServerIDChan <- true
				currentConnection = <-(*opt).getNewServerIDChan
			} else { //Just do it.
				currentConnection = newServerID
			}
			//Try loading connection again.
			var _, found = (*(*opt).connections).Load(currentConnection)
			if !found {
				//So that request to connect can be send if its not found
				//Modified
				(*opt).requestConnectionChan <- raft.RequestConnection{Id: currentConnection, Destination: (*opt).connections}
				//eModified
			}
		//handleActionResponse calls this channel with the action result (applied / not applied)
		case done := <-actionDoneChannel:
			clearToSend = true
			// if action was applied - and there is still something enqueued
			if done && len(actions) > 0 {
				//Modified
				//Check type to see if is disconnect.
				if actions[0].Type == "Disconnect" {
					//In which case puts in channel that is disconnected.
					(*opt).removedFromGameChan <- true
				}
				//-Modified
				//Removes that same first action by (putting it in last place -> slicing to semi-last position)
				copy(actions, actions[1:])
				actions = actions[:len(actions)-1]
			}
		//After 10 milliseconds
		case <-time.After(time.Millisecond * 10):
			if clearToSend && len(actions) > 0 { //ClearToSend is only changed to true once message is removed from queue (actions)
				clearToSend = false
				var msg = actions[0] //Gets first actions
				var timestamp = getNowMs()
				var actionResponse raft.ActionResponse
				var jsonAction, _ = json.Marshal(msg.Action) //Marshal it to JSON of GameLog
				//Modified
				//Puts it in a raft Action (same as gamelog, but Action is actually a JSON of GameLog ...WTF...)
				var actionArgs = raft.ActionArgs{Id: string(msg.Id), ActionId: msg.ActionId, Type: msg.Type, Action: jsonAction}
				//eModified
				//Try loading connection
				var conn, found = (*(*opt).connections).Load(currentConnection)
				if !found { //If *_SOMEHOW_* still couldnt load connection, starts a new one (postergating message to next 10 milliseconds)
					//Modified
					(*opt).requestConnectionChan <- raft.RequestConnection{Id: currentConnection, Destination: (*opt).connections}
					//eModified
					(*opt).requestNewServerIDChan <- true
					currentConnection = <-(*opt).getNewServerIDChan
					clearToSend = true
				} else { // If connection loaded, sends message to Raft by the ActionRPC.
					var raftConn = conn.(raft.RaftConnection)
					actionCall := raftConn.Connection.Go("RaftListener.ActionRPC", &actionArgs, &actionResponse, nil)
					//And handle the response of that call
					go handleActionResponse(actionCall, &actionResponse, changeConnectionChan, msg, timestamp, currentConnection, opt, actionDoneChannel)
				}
			}
		}
		/* If game is too slow could make this below, just remember to sleep this signal about 10 milliseconds
		if clearToSend && len(actions) > 0 { //ClearToSend is only changed to true once message is removed from queue (actions)
			clearToSend = false
			var msg = actions[0] //Gets first actions
			var timestamp = getNowMs()
			var actionResponse raft.ActionResponse
			var jsonAction, _ = json.Marshal(msg.Action) //Marshal it to JSON of GameLog
			//Modified
			//Puts it in a raft Action (same as gamelog, but Action is actually a JSON of GameLog ...WTF...)
			var actionArgs = raft.ActionArgs{Id: string(msg.Id), ActionId: msg.ActionId, Type: msg.Type, Action: jsonAction}
			//eModified
			//Try loading connection
			var conn, found = (*(*opt).connections).Load(currentConnection)
			if !found { //If *_SOMEHOW_* still couldnt load connection, starts a new one (postergating message to next 10 milliseconds)
				//Modified
				(*opt).requestConnectionChan <- raft.RequestConnection{Id: currentConnection, Destination: (*opt).connections}
				//eModified
				(*opt).requestNewServerIDChan <- true
				currentConnection = <-(*opt).getNewServerIDChan
				clearToSend = true
			} else { // If connection loaded, sends message to Raft by the ActionRPC.
				var raftConn = conn.(raft.RaftConnection)
				actionCall := raftConn.Connection.Go("RaftListener.ActionRPC", &actionArgs, &actionResponse, nil)
				//And handle the response of that call
				go handleActionResponse(actionCall, &actionResponse, changeConnectionChan, msg, timestamp, currentConnection, opt, actionDoneChannel)
			}
		}
		*/
	}
}

// TermChan is a channel for O.S. process termination, so this function safelly exits if windows was closed.
func handlePrematureTermination(termChan chan os.Signal, connectedChan chan bool) {
	select {
	case <-termChan:
		log.Info("Main - Shutting down before full connection...")
		os.Exit(0)
	case <-connectedChan:
	}
}

// Send data back to unity
func SendData(write bufio.Writer, actionChan chan raft.GameLog) {

	//Only for coehesion in debugging
	typeToString := map[int]string{
		None:    "None",
		Take:    "Take",
		Shoot:   "Shoot",
		Melee:   "Melee",
		Special: "Special",
		Super:   "Super",
		Damage:  "Damage",
		Spawn:   "Spawn",
		Die:     "Die",
	}

	logFile, err := os.Create("log.txt")
	if err != nil {
		log.Error(err)
		os.Exit(1)
	}
	defer logFile.Close()

	for {
		//Every time an action is flushed to channel (by Raft Listener)
		recvAction := <-actionChan

		var action ActionImpl
		json.Unmarshal(recvAction.Action, &action)

		actionId = int(recvAction.ActionId)

		newAction := GameLog{Id: PlayerID(recvAction.Id), ActionId: recvAction.ActionId, Type: recvAction.Type, Action: action}

		sendMsg, err := json.Marshal(newAction)

		//Debug
		recvMessage := fmt.Sprint("Sending: { PlayerID: ", newAction.Id, " ", "ActionID: ", newAction.ActionId, " ", "Type: ", newAction.Type, " ",
			"Action { ",
			"Position: ", newAction.Action.Position, " ",
			"Rotation: ", newAction.Action.Rotation, " ",
			"Type: ", typeToString[newAction.Action.Type], " ",
			"Args: ", newAction.Action.Arg, " } }")
		log.Info(recvMessage)
		logFile.WriteString(recvMessage + "\n")

		if err == nil {
			buf := make([]byte, 512)
			copy(buf, sendMsg)
			write.Write(buf)
			write.Flush()

		} else {
			log.Error(err)
		}
	}
}

// Receive data from unity
func RecvData(reader bufio.Reader, opt *options) {
	for {
		//Read 1024 bytes from pipe (coming from unity)
		buffer := make([]byte, 512)
		readCount, err := reader.Read(buffer)
		if err != nil {
			log.Error(err)
			continue
		}

		//Creates a GameLog var, and if message is actually filled
		var action GameLog
		if readCount > 0 {
			//Fills gamelog to replicate
			formatedData := strings.ReplaceAll(string(buffer[:]), "\x00", "")

			err = json.Unmarshal([]byte(formatedData), &action)
			if err != nil {
				//log.Error(err)
				continue
			}

			//Flush to channel so Raft part happens.
			(*opt).actionChan <- action
		}
	}
}

func main() {
	//Only or logger functions, can remove if not needed
	log.SetLevel(log.InfoLevel)
	customFormatter := new(log.TextFormatter)
	customFormatter.TimestampFormat = "2006-01-02 15:04:05.000000"
	log.SetFormatter(customFormatter)
	//--------------------------------------------------

	termChan := make(chan os.Signal, 1) //Creates a signal for process ending
	signal.Notify(termChan, syscall.SIGTERM, syscall.SIGINT, syscall.SIGABRT, syscall.SIGQUIT, syscall.SIGALRM, syscall.SIGBUS, syscall.SIGHUP, syscall.SIGQUIT, syscall.SIGKILL)
	connectedChan := make(chan bool) //Channel for connecting
	// Command line arguments (not really needed as unity does this part)
	args := os.Args
	if len(args) < 2 {
		log.Fatal("Usage: <NamedPipeName> <localIP> []<External IPs>")
	}

	pipeName := args[1]
	localIP := args[2]

	//Assert path to named pipes (by S.O.)
	d := 10 * time.Second
	osStr := runtime.GOOS
	var pipePath string
	switch osStr {
	case "windows":
		pipePath = `\\.\pipe\%s`
	case "linux":
		pipePath = ``
	default:
		pipePath = ""
	}

	//Case termChan (process signal) shows that program ended before finalizing connection, this functions log and safely quits
	go handlePrematureTermination(termChan, connectedChan)

	//ServerID is just a string containing the identification on a raft node
	var playerID = PlayerID(localIP)
	var serverID = raft.ServerID(localIP)
	var firstLeader = raft.ServerID("")

	//Make 0 slots array
	otherServers := make([]raft.ServerID, 0)
	for i, arg := range args {
		if i > 2 {
			//If any, append to array
			otherServers = append(otherServers, raft.ServerID(arg))
		}
	}

	//In case you append to another ip, that one (first), is considered alredy the leader (if this isn't right, raft logic corrects automatically)
	if len(otherServers) > 0 {
		firstLeader = otherServers[0]
	} else {
		//You are the leader!
		firstLeader = serverID
	}

	// Client nodeMode: UI + Engine + Raft node
	//All channels used to comunicate engine/ui/raft
	var mainConnectedChan = make(chan bool)                       //main channel of communication between scripts (in this case only this.), for connection of client
	var mainDisconnectedChan = make(chan bool)                    //main channel of communication between scripts (in this case only this.), for disconnection of client
	var nodeConnectedChan = make(chan bool)                       //Communication of state of connection in raft network
	var uiActionChan = make(chan GameLog)                         //Needed for receiving controls from UI.go (now only for pipe recv)
	var uiConfChan = make(chan bool)                              //Possibly not needed... (but is used for comunicating with UI.go --- setup, conn)
	var actionChan = make(chan raft.GameLog)                      //Channel of action, originally for communicating engine-UI-main-Raft, now used for main-Raft-unity
	var snapshotRequestChan = make(chan bool)                     //Request new snapshot of system
	var snapshotResponseChan = make(chan []byte)                  //Receive new snapshot of system
	var snapshotInstallChan = make(chan []byte)                   //Apply in self new snapshot of system
	var requestConnectionChan = make(chan raft.RequestConnection) //Request Raft network schema, the Request Connections takes a map of connections
	var requestNewServerIDChan = make(chan bool)                  //Request a new serverID (playerID, but global)
	var getNewServerIDChan = make(chan raft.ServerID)             //Channel for receiving this serverID.
	var removedFromGameChan = make(chan bool)                     // When other clients have removed you from their connections

	//Initiate connection to game here (pipe and writer-reader)
	log.Info("Connecting to pipe: " + fmt.Sprintf(pipePath, pipeName))
	conn, err := winio.DialPipe(fmt.Sprintf(pipePath, pipeName), &d)
	if err != nil {
		fmt.Println(err)
		return
	}
	defer conn.Close()
	log.Info("Connected to pipe: " + conn.LocalAddr().String())

	reader := bufio.NewReader(conn)
	writer := bufio.NewWriter(conn)
	writer.Flush()

	//LocalIP  other nodes IP Channel for engine new gamelogs
	var _ = raft.Start(localIP, otherServers, actionChan, nodeConnectedChan, snapshotRequestChan, snapshotResponseChan, snapshotInstallChan) //raftLogic.go -> Start
	var nodeConnections = raft.ConnectToRaftServers(nil, raft.ServerID(localIP), otherServers)                                               //raftLogic.go -> ConnectToRaftServers

	//Encapsulate all channels to easy passage to go routines, etc... (instead of using N params, use one)
	var opt = options{
		uiActionChan,
		uiConfChan,
		firstLeader,
		nodeConnections,
		otherServers,
		serverID,
		mainConnectedChan,
		mainDisconnectedChan,
		requestConnectionChan,
		requestNewServerIDChan,
		getNewServerIDChan,
		removedFromGameChan}

	//Analyse if needed
	go connectionPool(&opt) //Keeps track of newServerID requests
	go raft.ConnectionManager(nil, requestConnectionChan)
	go manageActions(&opt) //Keeps track of new actions to apply->replicate and response of them

	//Start data replication of and to Unity **as this was point UI was initialized
	log.Info("Starting data replication")

	//SendData and RecvData -> Send gamelogs to unity (from raft) and Receive gamelogs of player (from unity)
	go SendData(*writer, actionChan) //Uses the same actionChan that is integrated to raft Listener -- Must be started before connection, as followers receive all that has already happened, by the leader
	go RecvData(*reader, &opt)

	//Removed other connections modes, only using Append (For now)
	if len(otherServers) > 0 {
		// Wait for the node to be fully connected
		uiConfChan <- true
		<-mainConnectedChan
	}
	nodeConnectedChan <- true

	// Finally, after connecting and applying all happened logs, send self connect (to instantiate prefab, spawn player, etc...)
	acImpl := ActionImpl{Position: Vector3{X: 0, Y: 0, Z: 0}, Rotation: Quaternion{W: 0, X: 0, Y: 0, Z: 0}, Type: 0, Arg: ""}
	uiActionChan <- GameLog{Id: playerID, ActionId: 1, Type: "Connect", Action: acImpl}

	connectedChan <- true
	<-termChan //Bash/CMD/Process windows closing
	// Completlly shut down raft and connections (or timeout...)
	log.Info("Main - Shutting down")
	uiActionChan <- GameLog{Id: playerID, ActionId: int64(actionId + 1), Type: "Disconnect", Action: acImpl}
	<-removedFromGameChan
	uiConfChan <- false
	select {
	case <-mainDisconnectedChan:
		log.Info("Main - Shutdown complete (clean)")
	case <-time.After(time.Millisecond * 5000):
		log.Info("Main - Shutdown complete (timeout)")
	}
}

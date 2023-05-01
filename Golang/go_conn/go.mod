module go_conn

go 1.19

replace go_raft => ./go_raft

require (
	github.com/Microsoft/go-winio v0.6.0
	github.com/sirupsen/logrus v1.9.0
	go_raft v0.0.0-00010101000000-000000000000
)

require (
	golang.org/x/mod v0.6.0-dev.0.20220419223038-86c51ed26bb4 // indirect
	golang.org/x/sys v0.0.0-20220722155257-8c9f86f7a55f // indirect
	golang.org/x/tools v0.1.12 // indirect
)

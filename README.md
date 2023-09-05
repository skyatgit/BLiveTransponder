# BLiveTransponder

B站直播间弹幕消息转发器

本项目为Godot项目

本项目依赖于 https://github.com/skyatgit/BLiveAPI

程序启动时自动在 http://localhost:19980/BLiveSMS/ 开启WebSocket服务并转发SMS消息

具体功能：

1. 提供直播中可能会用到的一些基本功能
2. 在本地开启一个WebSocket服务端,转发从弹幕服务器获取到的所有SMS消息
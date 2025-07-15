# 多人游戏

多人游戏是本项目的核心特性之一，展示了如何快速集成平台的多人游戏功能，构建一个便于与他人和朋友互动的项目。

## 玩家加入游戏的方式

我们提供了多种加入或创建游戏的方式。在主菜单中有4个选项可以让我们进入游戏。

![主菜单](./Media/mainmenu.jpg)

让我们深入了解这些不同的选项：快速匹配、主机匹配、观战匹配和好友。

### 快速匹配

快速匹配按钮会启动一个流程，我们将尝试通过Photon随机房间API作为客户端加入一个随机房间。如果找不到房间，我们会作为主机创建一个新房间。

在此模式下，任何人都可以在有空位时加入。一旦房间被填满，试图加入的额外玩家将创建一个新房间。

### 主机匹配

这会创建一个私人竞技场，只有你的朋友可以作为观众或玩家加入。他们可以通过好友菜单加入（我们将在下面介绍）或通过邀请加入。

### 观战匹配

加入一个由快速匹配创建的随机竞技场进行观战。你将坐在看台上，可以为你喜欢的队伍加油。

### 好友

好友菜单将列出所有可以加入游戏或观战的好友。

![好友菜单](./Media/mainmenu_friends.png)

通过点击观战或加入，将启动作为客户端加入房间的流程。

[FriendsMenuController](../Assets/PongHub/Scripts/MainMenu/FriendsMenuController.cs) 将触发适当的导航流程。

## 导航流程

负责导航流程的核心元素是 [NavigationController](../Assets/PongHub/Scripts/App/NavigationController.cs)。

这里有不同的API来导航应用程序。简而言之，它将通过 [NetworkLayer](../Packages/com.meta.multiplayer.netcode-photon/Core/NetworkLayer.cs) 启动连接流程，并通过 [PlayerPresenceHandler](../Assets/PongHub/Scripts/App/PlayerPresenceHandler.cs) 设置新的群组状态。

设置群组状态将使其他用户知道玩家在哪里，是否可以加入他们以及在哪里加入。

一旦建立连接，[NetworkStateHandler](../Assets/PongHub/Scripts/App/NetworkStateHandler.cs) 处理网络状态的变化。这是玩家连接或断开连接时进行设置的地方。

在连接时，我们还使用 [SceneLoader](../Packages/com.meta.multiplayer.netcode-photon/Core/SceneLoader.cs) 导航到正确的场景。场景加载器处理场景的状态以及如何在netcode网络状态下处理场景导航。当通过netcode连接时，我们使用netcode场景管理器在用户之间同步场景。

## 群组启动和邀请

当用户启动群组启动到竞技场或用户相互邀请到现有竞技场时，我们在 [PHApplication](../Assets/PongHub/Scripts/App/PHApplication.cs) 中处理这些情况。这是我们检查意图信息并基于该信息触发适当导航流程的地方。

对于这个项目，所需的核心信息是目标API和lobbySessionId。lobbySessionId用作要加入的房间名称，目标API将指示要加入的区域。一旦我们处理了该信息，我们调用导航控制器，它将带我们到正确的竞技场并设置我们的群组状态。

## 语音通话

[VoipController](../Packages/com.meta.multiplayer.netcode-photon/Core/VoipController.cs) 处理玩家语音通话扬声器和录音器的设置。录音器将录制本地玩家的声音并通过网络发送，在其他客户端上将创建扬声器来播放接收到的声音。它还处理检查平台录音音频的权限。

[VoipHandler](../Packages/com.meta.multiplayer.netcode-photon/Core/VoipHandler.cs) 跟踪给定实体的录音器或扬声器，通过保持对录音器和扬声器的引用，使得静音和取消静音扬声器或停止和开始录音变得更容易。这被下面指定的静音行为使用。

## 屏蔽流程和静音

在游戏中，玩家可能会变得令人讨厌，或者在快速匹配中我们可能遇到已经屏蔽的用户。

为了处理这种情况，我们集成了平台API的屏蔽流程。核心实现可以在 [BlockUserManager](../Packages/com.meta.multiplayer.netcode-photon/Core/BlockUserManager.cs) 中找到。

在初始化时，我们获取当前用户已屏蔽的所有用户列表，这样我们就可以跟踪这些用户，以便在遇到他们时能够处理。这个类还实现了触发屏蔽和取消屏蔽流程，并将跟踪所有被屏蔽用户的状态。

当用户在游戏中且玩家生成时，我们检查该玩家是否被该用户屏蔽，如果是，我们将其静音。我们实现了 [UserMutingManager](../Assets/PongHub/Scripts/App/UserMutingManager.cs) 来跟踪我们在游戏中静音的用户。这样我们可以在会话期间静音他们，但不在平台级别屏蔽他们。

当玩家的 [PlayerStateNetwork](../Assets/PongHub/Scripts/Arena/Player/PlayerStateNetwork.cs) 使用该playerID更新或静音状态发生变化时，它会更新该玩家的扬声器以静音他们。我们还在玩家菜单 [PlayerInfoItem](../Assets/PongHub/Scripts/Arena/Player/Menu/PlayerInfoItem.cs) 中处理屏蔽流程。

![游戏内菜单玩家](./Media/ingamemenu_players.jpg)

## 邀请流程

在游戏中和游戏前阶段，有一个按钮可以邀请玩家加入竞技场并游戏。这个按钮将从 [GameManager](../Assets/PongHub/Scripts/Arena/Gameplay/GameManager.cs) 触发邀请流程。

![游戏内邀请](./Media/ingameinvite.jpg)

邀请将通过平台发送。在接收端，它将按照[群组启动和邀请](#群组启动和邀请)部分中描述的方式处理。

## 花名册

在游戏中，玩家会遇到其他玩家，陌生人、朋友的朋友等。如果玩家与其他人玩得很开心，快速与他们成为朋友是很好的。这就是花名册面板发挥作用的地方。从游戏内菜单的玩家部分，我们处理花名册面板。有关实现详细信息，请参见 [PlayersMenu](../Assets/PongHub/Scripts/Arena/Player/Menu/PlayersMenu.cs)。

## 竞技场审批

由于我们有不同类型的加入方式，我们需要确保玩家会加入有空间的竞技场。

这通过2种不同的方式完成。

首先，我们使用Photon房间属性来设置房间是否对玩家和观众可用。如果房间对其中一个满了，我们将该标志设置为满。这样寻找快速匹配或观看随机匹配的玩家不会被发送到满员的竞技场房间。实现可以在 [PhotonConnectionHandler](../Assets/PongHub/Scripts/App/PhotonConnectionHandler.cs) 中看到，我们在加入或创建房间时设置要查找的属性，以及 [ArenaApprovalController](../Assets/PongHub/Scripts/Arena/Services/ArenaApprovalController.cs)，我们在其中更新房间的属性。

这带我们到第二个检查，[ArenaApprovalController](../Assets/PongHub/Scripts/Arena/Services/ArenaApprovalController.cs)。这个类实现了netcode的审批检查。它仅在主机上运行，并跟踪每种类型加入的用户数量。如果尝试连接的用户超过允许的数量，它将断开该玩家的连接并发送原因。这主要是为了处理玩家A试图加入他们的朋友玩家B但竞技场已满的情况。

## 群组状态

如上所述，当用户更改场景和网络房间时，我们设置群组状态，以便其他人可以轻松加入他们。主要实现在 [GroupPresenceState](../Packages/com.meta.multiplayer.netcode-photon/Core/GroupPresenceState.cs) 中完成，它实现了群组状态API并在本地保持状态以供参考。

## Photon与GameObject的Netcode

为了开发这个项目，我们使用了Photon和GameObject的Netcode。已经有一个相当好的photon包装器将其集成为netcode的传输层。但对于我们想要做的项目，我们发现它缺少一些关键的photon功能实现。

我们在[这里](../Packages/com.community.netcode.transport.photon-realtime@b28923aa5d)复制了包并实现了一些额外功能。我们试图保持项目无关性，以便可以轻松地重用于其他项目。

一些核心差异：

- 在连接时添加意图，作为客户端、主机或到大厅。[PhotonRealtimeTransport](../Packages/com.community.netcode.transport.photon-realtime@b28923aa5d/Runtime/PhotonRealtimeTransport.cs)
- 添加私人房间，我们在创建房间时设置可见标志。
- 添加区域覆盖以便能够更改区域。
- 支持加入随机房间失败。[PhotonRealtimeTransport](../Packages/com.community.netcode.transport.photon-realtime@b28923aa5d/Runtime/PhotonRealtimeTransport.Matchmaking.cs)
- 添加函数调用以处理带参数的房间创建。参数获取函数可以在运行时挂钩到 [PhotonRealtimeTransport.Connection](../Packages/com.community.netcode.transport.photon-realtime@b28923aa5d/Runtime/PhotonRealtimeTransport.Connection.cs)，并完全控制项目如何创建房间。

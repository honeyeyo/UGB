# 应用内购买 (IAP)

在商店中，您可以永久购买图标（耐用品）或购买一些宠物猫，这些宠物猫使用后需要重新购买（消耗品）。

![商店](./Media/mainmenu_store.png)

![商店购买流程](./Media/mainmenu_store_flow.png)

## 实现

我们的实现基于[IAP 开发者资源](https://developer.oculus.com/documentation/unity/ps-iap/)。

首先，我们有一个与游戏无关的 [IAPManager](../Assets/PongHub/Scripts/App/IAPManager.cs)，它封装了平台 IAP 逻辑，用于处理获取产品和购买信息以及消耗消耗品的购买。这旨在为任何想要实现 IAP 的项目类型提供可重用的逻辑。我们添加了对产品分类的支持，以便我们可以轻松获取给定类别的所有产品。

然后我们有 [StoreMenuController](../Assets/PongHub/Scripts/MainMenu/StoreMenuController.cs)，它实现了如何购买产品和消耗消耗品购买的逻辑。图标使用从 IAPManager 获取的产品数据加载。

获取产品和购买信息的逻辑可以在 [UGBApplication](../Assets/PongHub/Scripts/App/UGBApplication.cs) 的初始化代码中找到。我们在其中获取与不同类别关联的消耗品和耐用品。

## 配置

详细信息请参见[配置页面的附加组件部分](Configuration.md#add-ons)。

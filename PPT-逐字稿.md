# 2D Roguelike 地牢游戏 — 技术答辩逐字稿

---

## 第1页：封面

**标题：** 2D Roguelike 地牢游戏 — 程序化生成与事件驱动架构

**副标题：** 基于 Unity 引擎的 Roguelike 地下城技术实现

各位老师好，我今天答辩的题目是"2D Roguelike 地牢游戏——程序化生成与事件驱动架构"。本项目使用 Unity 引擎、C# 语言，从零实现了一款完整的 Roguelike 地牢探险游戏，共计 104 个 C# 脚本、约一万行代码，在三个半星期内完成从架构搭建到全流程闭环。我将从架构设计、核心算法、系统实现三个层面展开介绍。

---

## 第2页：总体技术架构

**要点（架构图）：**
- 六模块划分：Core / Dungeon / Characters / Data / UI / Debug
- 事件总线解耦
- Config → Runtime → Save 单向数据流

项目采用六模块架构。Core 层承载状态机和事件总线，是整个系统的"脊柱"；Dungeon 层负责地牢生成、视图实例化、迷雾与小地图；Characters 层处理玩家与敌人的战斗逻辑；Data 层实现 Config / Runtime / Save 三层数据分离；UI 层独立于游戏逻辑。

模块间通信遵循严格的"事件驱动"原则——任意模块只通过 EventCenter 发布和订阅类型化事件，不直接持有其他模块的引用。这带来两个好处：一是模块可独立测试，二是新增功能只需要订阅事件，不需要修改已有代码。数据流向上，Config 是只读的 ScriptableObject 配置，Runtime 是运行中可变状态，Save 是序列化到磁盘的 JSON 存档——三者单向流动，不可反向依赖。

---

## 第3页：核心架构——状态机与事件系统

**要点：**
- GameManager 白名单状态迁移矩阵
- EventCenter 类型化事件广播
- 18 种游戏事件 / 8 种游戏状态

先说状态机。GameManager 维护一个白名单迁移矩阵——这是一个硬编码的 Dictionary，Key 为当前状态，Value 为该状态允许迁移到的合法目标状态集合。比如 Hub 状态只能迁移到 RunInit，RoomPlaying 可以迁移到 RoomClear、BossPlaying 或 RunEnd。任何非法迁移会被 ChangeState 方法静默拒绝。同时，非活跃状态——Boot、RunInit、RoomClear、RewardSelect、RunEnd——会自动设置 Time.timeScale 为零，让游戏逻辑暂停，只在 Hub、RoomPlaying、BossPlaying、RewardSelect 四个阶段正常流动时间。

再说事件系统。EventCenter 是一个基于委托的事件总线，Key 是 GameEventType 枚举，目前定义了 18 种游戏事件——包括 GameStateChanged、PlayerDamaged、EnemyDied、RoomCleared、BuffApplied 等。支持零参、一参、两参三种泛型委托，并在注册时做运行时类型校验，防止参数签名不匹配。所有事件广播和订阅都通过强类型的 EventCenter.Broadcast 和 AddListener，杜绝裸 object 传参。

---

## 第4页：数据架构

**要点：**
- Config 层：ScriptableObject 只读配置
- Runtime 层：RunState POCO 运行时数据
- Save 层：JsonUtility + persistentDataPath 持久化

数据架构遵循 Config → Runtime → Save 三层单向依赖。Config 层使用 Unity 的 ScriptableObject 体系，包括 FloorConfigSO（楼层配置：网格大小、房间数、形状权重、精英/事件数量）、BuffConfigSO（Buff 的数值修正与持续时间类型）、BuffPoolSO（全局奖励池，含各稀有度权重）。这些在 Editor 中可视化编辑，运行时只读。

Runtime 层以 RunState 为核心——这是一个标记为 Serializable 的纯数据 POCO，包含 RunId、Seed、FloorIndex、RoomIndex、ActiveBuffs 列表、Inventory 和 PendingReward。RunManager 在 RunInit 状态时创建或从存档恢复 RunState，RoomClear 时增量保存检查点。

Save 层使用 JsonUtility 序列化到 Application.persistentDataPath。支持泛型 SaveRaw 处理任意可序列化对象，Save 则要求实现 ISaveData 接口做版本校验。续关存档在 RunEnd 后自动删除，防止脏数据残留。

---

## 第5页：地牢生成流水线（上）

**要点：**
- 十步确定性流水线
- 多源交替 BFS 足迹构建
- 方向偏置加权 DFS 生成树

这是本项目技术含量最高的部分。地牢生成由 DungeonGenerator.Generate(seed, config) 方法驱动，执行一个严格的十步流水线，每一步都完全可复现。

第一步验证配置——检查 FloorConfigSO 的网格大小、房间数、特殊房间预算、模板是否存在。第二步，使用 FNV-1a 哈希从全局种子派生两个子 RNG：layoutRng 用于拓扑结构，contentRng 用于内容分配——这意味着布局随机和内容随机互不干扰，可以独立调试。第三步锚点规划，从 4 种对角方案中随机选择一个，决定起点和 Boss 房间在网格中的位置。

第四步是最关键的——FootprintBuilder 执行多源交替 BFS。起点区域和 Boss 区域同时作为 BFS 源向外生长，每次各取一个随机前沿单元格扩展。当两个生长前沿相遇时，前沿合并，继续扩展直到达到配置的房间总数。这个算法的优点是：保证起点和 Boss 之间必定连通；路径长度自然由 BFS 的生长过程决定，形成有机的地图拓扑。

---

## 第6页：地牢生成流水线（下）

**要点：**
- 生成树 → 合并 → 特殊房间 → 模板选择 → 门匹配
- Boss 单入口门保证
- L 形 / 双格矩形合并

第五步，SpanningTreeBuilder 在 BFS 足迹上构建生成树。使用方向偏置的加权 DFS：朝向 Boss 方向的邻居获得 2.0 倍的随机权重，使得主路径趋向于从起点向 Boss 方向延伸。同时强制执行 Boss 区域的单入口约束。

第六步到第八步是房间合并与特殊房间分配。Boss 的 2x2 四格会合并为一个 BigSquare 大房间。SpecialRoomAssigner 沿主路径的 BFS 最短路径分配精英房间——优先放在主路径后半段，事件房间优先放在分支节点上，确保它们不在必经之路上，增加探索价值。Normal 房间根据 ShapeWeight 权重表按概率合并为 L 形或双格矩形。

第九步模板选择，根据房间的形状和类型在模板池中加权随机选取。第十步 DungeonMap.Build 解析门连接——通过 WallEdge 标准化的门位匹配，将相邻房间的门两两配对，并验证 Boss 单入口不被破坏，最终输出不可变的 RoomInstance 数组。

---

## 第7页：房间系统与类型

**要点：**
- 5 种房间类型 / 8 种房间形状
- IRoomBehavior 策略模式
- DoorSlot 标准化门匹配

房间类型枚举定义了 Start、Normal、Elite、Event、Boss 五种类型，每种对应不同的行为策略。这是通过 IRoomBehavior 接口实现的——NormalRoomBehavior 在玩家进入时锁门、清场后解锁；BossRoomBehavior 清场后额外激活传送门并触发奖励阶段；StartRoomBehavior 不锁门；Event 房间复用了 Normal 的行为逻辑但提供三选一奖励。

房间形状有 8 种——Single 单格、DoubleH 水平双格、DoubleV 垂直双格、BigSquare 四格大房间，以及 4 种 L 形变体（左上、左下、右上、右下）。RoomShapeUtil.GetCells() 将每种形状映射到其相对单元格坐标，生成器据此确定房间占用的网格空间。

门匹配系统是连接生成层和视图层的关键——每个 RoomTemplateSO 定义了该模板上可用的 DoorSlot，包括位置、朝向。DungeonMap.Build 时，相邻房间的门位在 WallEdge 规范化的坐标系下两两匹配，生成 ConnectedDoor 引用对，供 DoorTransitCoordinator 在运行时建立传送链路。

---

## 第8页：确定性随机系统

**要点：**
- System.Random 封装
- FNV-1a 哈希子种子派生
- Fisher-Yates 洗牌 / 累积权重选择

Roguelike 游戏的一个核心特性是可复现性——相同的种子应该生成完全相同的地牢。为了实现这一点，我封装了 System.Random 为 SeededRandom 类，提供 Range、Value、Shuffle、WeightedSelect 等常用随机操作。

但关键挑战在于子种子派生。如果整个生成流程共用一个 Random 实例，任何步骤的调用顺序变化都会改变后续步骤的结果，破坏确定性。解决方案是使用 FNV-1a 哈希算法，从全局种子和分类标签生成子种子——例如 layoutRng 用 Hash(seed, "layout")，contentRng 用 Hash(seed, "content")。FNV-1a 是确定性哈希，不依赖运行时的 string.GetHashCode，保证跨平台一致性。

每层的种子使用 Hash(globalSeed, floorIndex) 派生，使得不同层的随机独立。奖励掉落种子进一步加入 roomId——Hash(runSeed + roomId, "reward")——保证同一层同一房间的奖励在重新进入时保持一致。

---

## 第9页：战争迷雾系统

**要点：**
- Hidden / Silhouette / Revealed 三态
- 不可回退规则
- MaterialPropertyBlock 高效渲染

迷雾系统使用三态模型。Hidden 状态下房间 GameObject 完全禁用；Silhouette 是半透明剪影——通过 SimpleFogController 使用 MaterialPropertyBlock 为房间的所有 SpriteRenderer 设置暗色调（RGB 0.15, 0.15, 0.2），在不增加 Draw Call 的前提下实现迷雾效果；Revealed 是完全可见。

核心规则是"不可回退"——一旦房间被设置为 Revealed，就不能再变回 Hidden 或 Silhouette。这由 RoomView.SetVisibility 方法强制执行。

迷雾的触发时机在 DungeonViewManager 中统一管理。OnDungeonGenerated 时，将起点房间设为 Revealed，其直接邻居设为 Silhouette。OnRoomEntered 时，将目标房间升级为 Revealed，并将其未访问邻居从 Hidden 提升到 Silhouette。这个设计保证玩家始终能看到当前房间的相邻出口方向，但看不到更远的内容，形成自然的探索节奏。

---

## 第10页：小地图系统

**要点：**
- UGUI 动态构建
- 以玩家当前位置为中心
- 线条可见性随房间清理状态变化

小地图由 FloorMinimapController 在运行时动态构建。它不是静态的 UI 预设，而是监听 DungeonGenerated、DungeonReady、RoomEntered 和 RoomCleared 四个事件，在 Canvas 上实时创建 Mask 面板、内容层、线条层和格子层的完整 UGUI 层级。

构建流程：首先遍历所有房间计算全局边界，根据 Canvas 尺寸计算缩放比例，使地图内容适配面板。然后以当前房间为中心放置每个房间的格子——当前房间白色，已揭示房间灰色，Boss 房间额外显示 Boss 图标。

门连接线有一个巧妙的可见性规则——连接线只在两个端点房间都已被清理后才显示。这给玩家提供了一种"安全区域"的视觉反馈：已清理的路径完全可见，未探索区域保持隐藏。实现上使用排序的边 Key 做无向去重，避免双向绘制。

---

## 第11页：战斗系统——敌人巡逻与攻击逻辑

**要点：**
- RandomMovement：正弦振荡巡逻，无需路径寻路
- EnemyShoot：距离检测 → 冷却射击 → 扇形弹幕
- 无状态机、轻量级组件化设计

敌人的巡逻和攻击逻辑采用轻量级组件化设计，每个敌人由 RandomMovement 和 EnemyShoot 两个独立组件协作驱动，不依赖状态机或路径寻路系统。

先说巡逻。RandomMovement 在 FixedUpdate 中使用正弦波叠加实现非规则振荡——X 轴使用 Sin、Y 轴使用 Cos，配合独立可调的频率（frequencyX=1、frequencyY=0.5）和振幅（amplitudeX=2、amplitudeY=1），使敌人围绕生成点做椭圆轨迹的周期性浮动。每个敌人在初始化时分配随机的 timeOffset，避免同屏敌人同步移动。位移通过 Rigidbody2D.MovePosition 执行，保证物理碰撞正确。这个设计的优势在于极低的运行时开销——没有 A* / NavMesh 的 CPU 负担，却产出了足够不规则的视觉移动效果。

再说攻击。EnemyShoot 每帧执行 PlayerDetection：通过 GameObject.FindWithTag 定位玩家，计算距离与检测半径（默认 5 单位）比较。当玩家进入检测范围，首次立即开火，之后按冷却间隔（默认 0.5 秒）持续射击。ShootAtPlayer 使用动态弹幕密度——子弹数量在最小值和 maxBulletCount 之间按距离比例插值，玩家越近弹幕越密集。每发子弹从 BulletPoolManager 的对象池获取，以扇形扩散（bulletSpreadAngle=3° 步进）射向玩家当前位置，实现"预判不足但覆盖面广"的弹幕效果。

这套组件化设计的妙处在于敌人类型的差异化完全通过 Inspector 参数实现——Bat 高移速低伤害、Spider 中速中伤、Ghost 慢速高血量、Scorpion 快速低血量——四种敌人共用同一套巡逻和攻击代码，不需要继承或多态。

---

## 第12页：Buff 与奖励架构

**要点：**
- Config → Roll → Spawn → Pickup → Apply → Stat 全链路
- ScriptableObject 配置驱动 / 事件解耦通信
- BuffInstance 随 RunState 持久化，支持断档恢复

Buff 与奖励系统构成了一条完整的 Config → Roll → Spawn → Pickup → Apply → Stat 数据链路，全部由 ScriptableObject 配置驱动、事件总线解耦各环节。

链路起点是 BuffConfigSO —— 在 Editor 中可视化配置每个 Buff 的属性修正（Flat 绝对值 / Percent 百分比，覆盖 MaxHP、Attack、Speed 等六种属性）和五种持续时间类型（Instant / Timed / RoomScoped / Permanent / Stack）。全局 BuffPoolSO 定义稀有度权重和候选池。

房间清场后，RewardRoller 从 BuffPoolSO 中按累积权重随机选择候选 —— Normal 和 Elite 房间 2 选 1，Event 房间 3 选 1，Boss 房间过滤最高稀有度。种子由 runSeed + roomId 派生，保证同一房间奖励可复现。掉落物由 RewardSpawner 采用三级回退策略放置 —— 预设标记点 → 格子随机（含玩家避让、最小间距约束） → 包围盒中心。

拾取环节由 ExclusivePickupGroup 保证互斥 —— 选择一个后同组其余自动回收。BuffManager 应用后，BuffInstance 存入 RunState.ActiveBuffs，随 RunState 整体序列化到存档。所有状态变更通过 BuffApplied / BuffExpired / BuffStackChanged 事件广播，携带 BuffSnapshot 快照，消费者无需查询 BuffManager。

属性聚合采用分层计算：BuffManager.GetTotalStatModifier 遍历所有激活 Buff，按 StatType × ModifyType 分组求和。永久 Buff 重复拾取使用指数衰减堆叠（sum = 1 + 0.7 + 0.7² + ...），实现边际收益递减。PlayerCharacter 订阅 Buff 事件后调用 RefreshAllStats，将聚合值推送到各子系统的实际属性上。

---

## 第13页：对象池与性能优化

**要点：**
- ObjectPool：Key 分组 / 容量上限 / 预热
- IPoolable 生命周期回调
- 房间视图复用

对象池是贯穿整个项目的基础设施。ObjectPool 使用按 Key 分组的 Queue 结构，每个 Key 有独立的父节点容器和可选的最大容量——超过容量时回收直接 Destroy 而非入池，防止内存膨胀。支持 Warmup 预热操作，在场景加载时预实例化指定数量。

IPoolable 接口定义了两个回调——OnPoolGet 在对象从池中取出时调用，用于重置状态；OnPoolRelease 在回收入池时调用，用于清理引用。子弹、敌人、Buff 掉落物、房间视图都实现了此接口。

房间视图的对象池复用是本项目最重要的性能优化之一。每个 RoomTemplateSO 对应一个池 Key（Room_{templateId}），DungeonViewManager 通过 InstantiateFloorAsync 分帧实例化——每实例化一个房间 yield 一次，将工作分摊到多帧。楼层切换时，旧层房间视图通过 ReleaseFloorSlot 回收入池，新层从池中取出复用，大幅减少 Instantiate/Destroy 开销。

---

## 第14页：双槽异步预加载

**要点：**
- DungeonMap[2] 乒乓切换
- PreloadFloorAsync 后台生成
- 黑屏过渡 + 视图切换

这是确保流畅楼层过渡的核心优化。DungeonManager 维护一个大小为 2 的 DungeonMap 数组，通过 activeMapIndex 标记当前活跃槽。当玩家在 Slot 0 游玩时，Slot 1 在后台预加载下一层。

预加载流程分两阶段。第一阶段在 OnDungeonReady 和 OnFloorCompleted 时触发，同步调用 DungeonGenerator.Generate() 生成完整的地牢数据结构——这一步是纯 CPU 计算，通常在 10ms 内完成。第二阶段通过协程 InstantiateFloorAsync 逐房间实例化——每个模板从对象池获取或新建，每完成一个 yield return null 让出主线程，避免卡顿。

传送触发时，PortalTransitCoordinator 执行完整的过渡序列：启动黑屏遮罩 → 在黑暗期间释放旧槽的房间视图回池 → 切换到新槽的活跃根节点 → 注册新槽的房间视图到索引 → 淡出黑屏 → 广播 DungeonReady。整个过渡在 6 帧内完成，玩家感知不到加载过程。

---

## 第15页：开发流程与总结

**要点：**
- OpenSpec 规约驱动开发
- 技术亮点回顾
- 未来展望

在开发流程上，本项目采用了 OpenSpec 规约驱动的开发方式。每个功能变更先写 Proposal 明确目标与验收标准，再写 Design 设计时序与数据契约，然后生成 Spec 定义可测试的需求，最后拆分为可独立验证的 Tasks。整个项目累计归档了 14 个规格化变更，覆盖从核心数据层到异步预加载的所有功能迭代。

回顾本项目的技术亮点：一是十步确定性程序化生成流水线，多源 BFS + 偏置 DFS 保证连通性和多样性；二是事件驱动解耦架构，18 种类型化事件、白名单状态机；三是数值体系——指数衰减 Buff 堆叠、确定性奖励掉落、三层数据单向流；四是性能优化——双槽预加载、分帧实例化、对象池全线复用。

未来可以进一步深化的方向：增加更多房间形状和敌人类型以提高内容多样性；引入 A* 路径验证地牢的可通行性；实现玩家 Build 多样性（不同武器、主动技能）；以及通过 Unity Test Framework 建立自动化回归测试，利用确定性种子验证生成正确性。

以上就是我的答辩内容，请各位老师指正。谢谢！

---

> **附录：逐字稿使用说明**
> 
> 每页文字量约 200-300 字，朗读时间约 40-60 秒，整场答辩约 12-15 分钟。
> 建议搭配以下视觉元素：架构图（第2页）、地牢生成动画（第5-6页）、
> 房间类型对比图（第7页）、迷雾效果截图（第9页）、小地图 GIF（第10页）、
> Buff 链路图（第12页）、性能对比数据（第14页）、开发时间线（第15页）。

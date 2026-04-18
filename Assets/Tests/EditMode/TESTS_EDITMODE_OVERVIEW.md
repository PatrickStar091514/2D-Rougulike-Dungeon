# EditMode 测试项目汇总（合并版）

## 1. 测试程序集信息

- **程序集**：`Assets/Tests/EditMode/Tests.EditMode.asmdef`
- **命名空间**：`RogueDungeon.Tests`
- **平台**：Editor（EditMode）
- **引用**：`Core`、`Data`、`UnityEngine.TestRunner`、`UnityEditor.TestRunner`

## 2. 覆盖概览

| 测试类 | 文件 | 用例数 | 主要覆盖对象 |
|---|---|---:|---|
| EventCenterTests | `Assets/Tests/EditMode/EventCenterTests.cs` | 12 | `EventCenter`、`GameEventType`、事件 Payload |
| GameManagerTests | `Assets/Tests/EditMode/GameManagerTests.cs` | 12 | `GameManager`、`GameState`、状态迁移与 timeScale |
| ObjectPoolTests | `Assets/Tests/EditMode/ObjectPoolTests.cs` | 12 | `ObjectPool`、`IPoolable`、容量与清理逻辑 |
| SaveManagerTests | `Assets/Tests/EditMode/SaveManagerTests.cs` | 12 | `SaveManager`、`ISaveData`、`SerializableKeyValue` |
| RunManagerTests | `Assets/Tests/EditMode/RunManagerTests.cs` | 1 | `RunManager`（RunEnd 清理语义） |

- **总计**：49 个 EditMode 用例

## 3. 各测试项目明细

## 3.1 EventCenterTests（12）

### 无参委托
1. `AddListener_NoArg_BroadcastInvokesHandler`：无参订阅后广播可触发。
2. `RemoveListener_NoArg_BroadcastDoesNotInvokeHandler`：退订后不再触发。
3. `Broadcast_NoArg_MultipleSubscribers_AllInvoked`：多订阅者全部触发。

### 单参数泛型委托
1. `AddListener_OneArg_BroadcastPassesArgument`：参数正确传递。
2. `RemoveListener_OneArg_BroadcastDoesNotInvokeHandler`：退订后不再接收参数事件。
3. `Broadcast_OneArg_StructPayload`：结构体 Payload 字段完整传递。

### 双参数泛型委托
1. `AddListener_TwoArg_BroadcastPassesBothArguments`：双参数完整传递。

### 类型安全校验
1. `AddListener_TypeConflict_ThrowsInvalidOperationException`：同事件不同签名冲突应抛异常。
2. `AddListener_SameType_DoesNotThrow`：同签名重复注册可合并。

### 清理与异常隔离
1. `Clear_RemovesAllListeners`：`Clear()` 后无残留订阅。
2. `Broadcast_HandlerThrows_OtherHandlersStillCalled`：单处理器抛错不阻断后续处理器。
3. `Broadcast_NoSubscribers_DoesNotThrow`：无订阅广播安全返回。

## 3.2 GameManagerTests（12）

### 初始状态
1. `InitialState_IsBoot`：初始化状态为 `Boot`。

### 合法迁移
1. `ChangeState_Boot_To_Hub_Succeeds`
2. `ChangeState_FullRunCycle`
3. `ChangeState_RoomPlaying_To_RunEnd_DirectDeath`

### 非法迁移
1. `ChangeState_IllegalTransition_StateUnchanged`
2. `ChangeState_Hub_To_RunEnd_IllegalTransition`

### 重复迁移
1. `ChangeState_SameState_Ignored`：重复切换被忽略且不重复广播。

### 事件广播
1. `ChangeState_BroadcastsGameStateChangedEvent`：状态切换后广播 `GameStateChangedEvent`。

### TimeScale 规则
1. `TimeScale_Hub_Is1`
2. `TimeScale_RoomPlaying_Is1`
3. `TimeScale_RunInit_Is0`
4. `TimeScale_RoomClear_Is0`

## 3.3 ObjectPoolTests（12）

### 获取与复用
1. `Get_EmptyPool_InstantiatesNewObject`：空池获取会实例化。
2. `Get_AfterRelease_ReusesObject`：回收后再次获取可复用。

### 回收行为
1. `Release_ObjectDeactivatedAndParented`：回收后失活并挂到池容器。

### 预热
1. `Warmup_PreInstantiatesObjects`：预热对象可直接取出。

### 容量控制
1. `Release_ExceedMaxCapacity_ObjectDestroyed`：超容量回收触发销毁。
2. `SetMaxCapacity_MinimumIs1`：容量下限校验。

### 清池
1. `ClearPool_RemovesAllPooledObjects`
2. `ClearPool_UnknownKey_DoesNotThrow`
3. `ClearAll_ClearsAllPools`

### IPoolable 回调
1. `Get_InvokesIPoolableOnPoolGet`
2. `Release_InvokesIPoolableOnPoolRelease`

### 分 key 容器
1. `Release_DifferentKeys_DifferentContainers`：不同 key 分组到不同容器。

## 3.4 SaveManagerTests（12）

### Save/Load（ISaveData）
1. `Save_And_Load_RoundTrip`：保存后读取一致。
2. `Load_VersionMismatch_ReturnsDefault`：版本不匹配返回默认值。
3. `Load_FileNotExist_ReturnsDefault`：缺失文件返回默认值。
4. `Load_CorruptedFile_ReturnsDefault`：损坏 JSON 返回默认值。

### SaveRaw/LoadRaw
1. `SaveRaw_And_LoadRaw_RoundTrip`
2. `LoadRaw_FileNotExist_ReturnsDefault`
3. `LoadRaw_CorruptedFile_ReturnsDefault`

### 存在性与删除
1. `HasSave_NoFile_ReturnsFalse`
2. `HasSave_AfterSave_ReturnsTrue`
3. `DeleteSave_RemovesFile`
4. `DeleteSave_FileNotExist_DoesNotThrow`

### 序列化兼容
1. `Save_SerializableKeyValue_PreservesData`：`SerializableKeyValue` round-trip 保真。

## 3.5 RunManagerTests（1）

1. `ChangeState_ToRunEnd_ClearsCurrentRunAndCheckpoint`  
   覆盖 Run 生命周期关键语义：  
   - `RunInit` 恢复/创建后可访问 `CurrentRun`  
   - `RoomClear` 保存检查点  
   - `RunEnd` 删除检查点并清空 `CurrentRun`

## 4. 当前测试执行结论

- 最近一次 EditMode 执行结果：**49/49 通过**。

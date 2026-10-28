# VAC 交接修改核查（2026-10-28）

## 来源与实际差异

- 仓库 `DonGCoco/VAC`，目标 PR 基础分支 `feature/vac-experiment`。
- 下载时最新实验分支 HEAD 与交接基准均为 `cc5f8f9d1979abcb6ec97d40a1884e89ee3a8f8e`，两者差异为空；未发现仓库 AGENTS.md。
- 工作分支 `codex/vac-handoff-validation`。已阅读 `README_交接说明.md`，仅用 `changes.patch` 应用四个文件，未复制 replacement_files。交接包的既往验证记录不作为本次实测结果。
- `ConditionController.cs`：Inspector 默认偏移 `(0,-0.40)` m，公开 StimulusRoot 供诊断。位置设在 BoardAnchor 平面内，局部 Z=0；位置与自身条件缩放分开。物理单位仍以祖先缩放为1为前提。
- `M1_SmokeTest.unity`：序列化偏移由 `(0,0)` 改为 `(0,-0.4)`；未改其它场景设置。
- `TetrisBoard.cs`：默认方块与边框改用共享双面网格，两侧顶点均 Z=0，独立法线，保留 primitive 默认材质，销毁 Board 时释放网格。自定义 blockPrefab 仍保留厚度逻辑；当前场景未指定该 prefab。
- `BoardDistanceCalibration.cs`：LOCK 时先 RefreshMeasurement；每秒输出三种追踪距离和缩放。诊断量未加入 CSV，未更换 Actual/READY 的定义。
- 本次额外增加 `Tools/check_vac_handoff.py`（可重复的离线参数／几何检查）和本文。未上传交接 ZIP、工作簿或参加者数据；未修改 main、未改实验候选数值。

## 二维码

保留脚本默认、场景序列化中的 `markerSizeMeters=0.12` 和 `estimateQrLength=false`。BoardRegistration 将两者写入 `QRSettings.QRLength` / `EstimateQRLength`。旧文档的5 cm属于早期测试记录，未据此修改。

12 cm打印边长已由团队确认，但现场仍须确认测量的是二维码图案本身、不含外侧白边；SDK 输入尺寸与实物范围必须一致。未进行实体尺寸实测。

## 参数表核对

本次最终核对来源为用户补充路径的 `VAC_Tetris_Depth_Judgement_Literature_Review_Template.xlsm`，只读提取，未运行宏、未编辑文件。以其 `Master Parameter Table` C（当前候选）、F（推荐）、G（决定）及 `Decision Tracker` 为准，不将推荐栏当成已经实施的配置。先前本地中文核查副本仅用于初查，以下状态已按指定原表修正。这里核对实现，不重新验证表内文献结论。

| 原表位置及决定 | 代码／资产／运行时核对 | 实施状态与待办 |
|---|---|---|
| 主表5：0.74 m，KEEP | ExperimentConfig.cs 与 .asset 均0.74；ConditionController 与 DataLogger 使用该配置 | 已实现名义焦平面计算；不代表测得实际焦平面或调节反应 |
| 主表6–8：0.8/1/1.5 m，PROVISIONAL | 代码默认与资产一致；GetTargetDistance→校准、条件缩放、正式流程日志 | 已实现候选；距离定义与三条件操作检验待冻结 |
| 主表9–10：VAC表达与解释，KEEP | ConditionController 有符号 Df−Dv；CSV 用绝对值 | 部分实现；符号／方向和物理距离解释需在分析中统一，不能据模型声称真实舒适度 |
| 主表11：±0.03 m，PROVISIONAL | 默认与资产0.03；READY 按锚点中心距比较；LOCK重新测量 | 候选已实现；未记录连续距离轨迹及超限时长，未验证追踪精度 |
| 主表12：跨条件视角，KEEP | visualAngleReferenceDistanceMeters=1；scale=名义条件距离/1，条件开始应用一次 | 原有实现保留；下移、偏轴及后续头动下不保证严格恒定视角，见下文 |
| 主表13：静态自定步速，KEEP | TetrisManager 无自动下落；显式 HardDrop；SetSoftDrop 无操作 | 已实现，输入／游戏执行仍待Unity与设备验证 |
| 主表14：15分钟，PROVISIONAL | 默认与资产900秒；BeginSession→RemainingSeconds→EndSession | 已实现候选；未冻结最佳暴露时长 |
| 主表15–16：T与A/B/C；PROVISIONAL／UNCERTAIN | 独立训练T；ParticipantSession 按轮次分配A/B/C，三组循环平衡条件顺序 | 分配已实现，序列等难性未验证 |
| 主表17：指标，PROVISIONAL | 已记录分数、消行、方块数、平均放置时间、顶出、时长及 lines_per_minute | 主次指标与“有效分钟”、中止／暂停／重开计时规则未冻结；当前是会话经过时间 |
| 主表18：较远目标二选一，KEEP | controller ray selectEntered→Submit；每试次首次有效响应推进 | 已实现结构；空击不产生试次响应日志，设备可用性待测 |
| 主表19–21：1m、0.97/1.03、总差0.06m，PROVISIONAL | 三项代码／资产一致；运行时reference±difference/2，与BoardAnchor独立 | 候选已实现；PEST预实验、多难度验证及实际距离留存未实施 |
| 主表22–23：水平中心距0.12m、直径0.04m＠1m，PROVISIONAL | 代码与资产一致；左右各偏0.06m；默认目标运行时创建，直径按0.97/1.03倍率变为0.0388/0.0412m | 候选已实现；场景左右目标引用为空，使用运行时默认；射线碰撞体放大2.5倍，不等同视觉球面 |
| 主表24：尺寸补偿，KEEP | targetReferenceScaleDistanceMeters=1；按目标前向深度缩放 | 原有补偿已实现；偏轴时是近似，见下文 |
| 主表25：当前8次、建议增加，PROVISIONAL | 正式8次；练习3次（代码与资产一致） | 保持当前数值，增加数量由团队／pilot决定，未擅填 |
| 主表26：正确率为主、总选择时间为次，KEEP | 保存correct及reaction_time_s；ShowTrial激活目标后取计时起点，首次有效Submit终止计时 | 部分实现；不是确认首帧可见的呈现时间，含射线动作；无超时、故障／重试和时间清理规则，未实现分析排名或删除规则 |
| 主表27：左右平衡随机并存种子，KEEP | BuildBalancedOrder：8次4左4右后洗牌；3次练习多出的侧随机 | 平衡与随机已实现；正确侧入CSV，但随机种子未保存，重复串未限制 |
| 主表28–30：每条件前后SSQ KEEP；主观指标PROVISIONAL；QoE UNCERTAIN | FormalExperimentFlowController提供前／后外部问卷检查点；无内置量表或逐题SSQ/QoE数据 | 流程检查点已实现；需确认问卷版本、题项、量尺、计分与时间记录，不能把点击继续视为已采集问卷 |
| 主表31–32：至少5分钟及恢复规则，PROVISIONAL | minimumRecoverySeconds代码默认300；资产未序列化该新增字段，使用字段初值；计时到后仍需实验员手动继续 | 已实现最低计时／人工继续，未自动采集症状、与基线比较、每5分钟复测、等待上限或终止；需团队确认 |
| 主表33–34：参加者筛查与跨条件控制，KEEP | ParticipantSession主要记录ID、组、顺序；软件版本与部分运行参数有日志 | 外部筛查、光照／佩戴／姿势／帧率／掉帧和距离轨迹尚无完整实现或本次验证，不因KEEP视为全部完成 |

配置优先级：场景组件引用同一 `ExperimentConfig.asset`（GUID `f17c9c148bae4df1bf2e6b717885b6cc`），表中数值由该 ScriptableObject 提供，场景不另序列化这些配置字段。代码未发现运行时重新写入这些数值；运行时读取配置、派生目标与缩放。资产还未序列化 `protocolVersion`、`warmupCondition`、`warmupTetrisDurationSeconds`，字段默认分别为M7、C2、180秒；不擅自把这些候选扩展为正式决定。旧 low/high=1/2m 兼容字段仍在，正式 M6/M7 路径使用 C1/C2/C3。

## 距离定义与数据解释

| 使用位置 | 当前量与风险 |
|---|---|
| BoardDistanceCalibration.ActualDistanceMeters／READY／LockedDistanceMeters | Main Camera 到 BoardAnchor 中心的欧氏直线距离。READY并非板面垂距或游戏中心距离。锁定后Actual继续随头位变化，不持续执行READY容差门控。 |
| ExperimenterBridge→监控Actual | 转发上述actual_m、locked_m；标签本身未指出是哪一种距离。 |
| ConditionController | 以GetTargetDistance的名义条件距作 scale=d/d_ref 和 Dv=1/d；不使用实时Actual、垂距或游戏中心距。游戏偏轴下不能直接声称保持严格角大小或精确会聚需求。 |
| FormalExperimentFlowController→DataLogger | 开始区块时把名义target与锁定锚点距写入上下文，events/blocks/tetris/depth_trials继承。session.csv记录配置，不是连续实际距离轨迹。 |
| TetrisManager.SampleViewingDistance | 锚定期间采样Actual，按unscaledDeltaTime加权得到mean/min/max_viewing_distance_m。它们仍是锚点中心距离统计。 |
| DataLogger VAC列 | `abs(1/focal−1/distance)`。target_vac_d用名义值，locked_vac_d用锁定锚点距，mean_vac_d用平均锚点距再取倒数；不是逐时刻VAC的平均。有符号控制器值与无符号CSV在当前远焦面条件下数值相同，定义仍不同。 |
| 深度判断距离 | ShowTrial按试次开始相机forward乘0.97/1.03m，right偏移±0.06m，随后世界固定；CSV near_depth_m/far_depth_m是前向分量，不是眼到目标的直线距或连续眼位测量。直线距约0.97185/1.03175m；按前向深度缩放是偏轴角大小补偿的近似。 |
| 深度CSV里的exposure VAC | 继承前一个Tetris暴露条件的target/locked，不能解释为深度目标自身的VAC。 |
| Tools/validate_vac_session.py | 检查CSV结构、数量、锁定距接近target等；校准校验额外允许0.002m余量。通过不证明几何、Unity输入、定位精度或VAC分析定义正确。仓库未发现完整统计推断分析脚本。 |

当眼位正对二维码中心、锚点单位缩放且板面法向距离分别0.80/1.00/1.50m时，下移0.40m的游戏中心距约0.8944/1.0770/1.5524m。反过来眼正对游戏中心时，锚点距更长；不能用一个固定差值纠正所有姿势。

新增 `VAC geometry` 每秒输出 eyeToAnchor、eyeToPlaneNormal、eyeToStimulusCenter、anchorScale、stimulusScale、condition、locked；它们是追踪估计。诊断只要求曾注册过板，未锁定但丢失二维码时可能仍使用上次位姿，不能将每条诊断当成新鲜有效观测。需结合READY状态、marker可见性及anchorVsLiveMarker日志判断。诊断没有进入CSV，也不构成独立标尺真值。

正式采集前团队须选择条件距离定义，统一眼位参考、尺量方法、READY、缩放、CSV列名／版本及VAC分析。当前保留现有定义，未用Z偏移补偿，也不能声称已修复真实深度误差。

## 本次验证结果

已完成：

1. `git apply --check`与补丁应用；`git diff --check`通过。分支基准无差异，未发生合并冲突。
2. `python Tools/check_vac_handoff.py`通过：14组默认／资产候选一致、12cm固定QR、场景偏移、LOCK先测距、保留Actual定义、网格全部Z=0且四个三角形非退化并朝向两侧。
3. 默认布局计算：20行×2cm，边框2mm，最大1.5倍缩放，游戏半高含框0.303m；游戏顶边距12cm二维码图案下沿0.037m。不含白边，不证明板面／视场足够。
4. 静态检查 Tetris 的移动边界／碰撞、旋转与水平wall kick、HardDrop、LockPiece重挂父节点并设Z=0、消行与上方行下移、顶出清盘与重开路径；这些游戏算法未被补丁改写。此为源码审查，未执行游戏交互。
5. 静态追踪后续流程：训练T→深度练习→三轮前问卷／校准／正式Tetris／深度／后问卷，轮间最低恢复计时与手动继续，最后完成及导出路径。未做运行时端到端验证。

未完成／失败：

- 项目记录 Unity `6000.2.15f1`；本机仅找到 `6000.3.24f1`与`6000.6.0f1`。已在仓库外独立副本尝试6000.3.24f1批处理编译，退出码1，未得到完整项目编译成功结果。日志包含许可访问token提示，亦有脚本构建启动记录，但无法据此确定退出根因。临时副本曾自动升级包，任何此类变化均未复制回工作分支。
- 未确认匹配版本的Unity完整编译、Android构建、渲染、双面照明／可见性、材质兼容性、Mesh生命周期、输入及真实Tetris消行等交互。
- 未连接ML2执行测试；未完成二维码位姿／空间锚点、实体尺量、真实深度误差、板边界或可用视场验证；未生成新的实测CSV。仓库旧设备测试记录属于交接前版本，不能作为本次补丁的设备通过证明。

## 头显现场操作

1. 使用项目记录版本Unity打开工作分支，等待包解析与编译；确认Console无错误，再按现有ML2设置构建Development APK。核对固定QR=0.12、自动估计关闭、blockPrefab为空、偏移(0,-0.40)、BoardAnchor和XR Origin及祖先缩放均1。不得把Unity副本自动升级结果未经审查提交。
2. 量最终二维码图案边长，明确不含白边；确认二维码与实体板共面、局部-Y确实朝下。完成佩戴校准，确定统一双眼中心近似参考，勿以头显外壳代替眼位。
3. 先确定拟采用的距离定义并记录。使用统一眼位参考，在0.80、1.00、1.50m各扫描至少3次；同步保存尺量值、三种VAC geometry距离、条件、缩放、marker／anchor状态及时间。分别记录中心直线距与平面垂距，不能混用。比较绝对及相对误差，排查参考点、二维码尺寸、姿态与漂移。
4. 每条件确认整块游戏位于二维码下方、不盖白边、顶点和边框共面，且不超实体板或可用视场；侧看双面是否显示正确。若方向不符仅调整平面内偏移；禁止用Z遮掩误差。
5. READY时点击LOCK；另测在READY后改变距离或遮住二维码再点LOCK应被拒绝。等待空间锚点真正tracking／LOCKED后由参加者START。轻微侧移和转头检查世界固定及anchorVsLiveMarker漂移，保存日志。
6. 检查左右移动边界、所有七种方块旋转／贴边kick、显式落下、锁定与下一块、多行消除和行下移、顶出清盘、重开。确认无自动重力、START触发未误落块、活动／锁定方块和框同平面。
7. 用专用测试ID跑训练及全部三轮正式流程，核对8次正式／3次练习、左右平衡、深度目标尺寸和选择、前后外部问卷、恢复人工决定。开发跳过仅用于软件冒烟，不能替代正式15分钟暴露或恢复验证，也不能用于正式数据。
8. 按现有热点监控流程确认状态、完成与自动导出。对新生成会话运行 `python Tools/validate_vac_session.py <session_folder>`，核对5个CSV、三轮Tetris、24条正式深度响应、session_id一致、距离列含义正确；额外保存诊断日志与尺量记录。验证器PASS仅表示其检查通过。

## 仍需团队决定

距离定义及眼位／实体标尺标准；三档与±3cm容差的操作检验；严格角大小控制方案及诊断入CSV／轨迹／超限记录；VAC符号、方向和平均方式；序列等难性、时长及有效时间指标；深度试次数、难度与可选PEST预实验；随机种子留存；响应起点、超时、分母、无效试次／故障／重试清理规则；SSQ与QoE版本和分析；恢复复测、个人基线、最大等待及终止规则；筛查排除标准和环境记录。原表的KEEP是设计决定，仍需区分其实现完整性和设备验证状态。

# 简介
下载、导出赛尔号 Unity 端的资源；顺便更新我的配置解析项目 SeerUnityTextParse
# 使用方式
1. Windows 安装 Visual Studio
2. 下载本项目源码
3. 按你的需求修改源码的 Main 部分后运行
## 修改方式
### 1. 下载最新版本资源
#### updateGame 的第二个参数
##### name 属性
参考游戏所在目录下的```Seer_Data\yoo```文件夹，name 属性对应这几大类资源的名称
##### list 属性
表示从某大类的资源中下载哪些文件，用正则表达式匹配
###### 获取所需资源路径
1. 可以先注释掉 Main 部分的其他代码，先运行一次 updateGame，即可在编译出来的 exe 所在目录下的```seer_download```文件夹中看到一些 json 文件，里面有各大类资源内部的所有资源路径。
2. 找所需资源的 Container 路径（分别将```yoo```文件夹中的子文件夹拖入另一个开源项目 AssetStudioGUI 中，在 Asset List 页查找所需资源的 Container 路径）
3. 例如群星牌的图片在 DefaultPackage 中，Container 路径为```assets/art/autocard/texture/cards/card_4.png```，则可以在```seer_download```文件夹的 DefaultPackage.json 文件中查找```assets_art_autocard_```，根据找到的文件名修改 list 参数
### 2. 导出
RunExportBatch 的第二个参数是数组，每个元素有 3 个子元素，分别是 [Container 路径, 导出路径, 导出方式]
#### 导出方式
- EXPORT_SKIP_EXISTING：跳过已存在的文件（适用于精灵头像这样很少改动原有文件的情况）
- EXPORT_CLEAR：清除导出文件夹后导出（适用于 Config 这样的每周改动原有文件的情况）
### 3. 反编译
基本不需要改，反编译结果放在 exe 所在目录下的```GameLogic```文件夹中
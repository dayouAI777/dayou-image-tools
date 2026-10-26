# 大有图片工具：版本发布与自动上传

本文记录本项目从改版本到 GitHub Release 自动发布的完整流程，避免每次重新配置或手动上传安装包。

## 当前发布方式

仓库中的 `.github/workflows/publish-release.yml` 会在 `main` 分支收到提交时自动运行。它从 `setup.iss` 读取版本号，检查同版本更新说明，构建主程序、升级器、`.dayouupdate` 更新包和 Windows 安装程序，然后创建 GitHub Release 并上传两个附件。发布时会自动标记为 Latest。

因此，正常发布不需要手工创建 Release、上传附件、选择 Latest 标签，也不需要另做 ZIP。用户可以从 [最新版下载页](https://github.com/dayouAI777/dayou-image-tools/releases/latest) 获取完整安装程序；安装后，日常版本可以从软件的更新中心获取。

## 陌生电脑安装与更新前提

普通用户不需要安装 Visual Studio、Python、Inno Setup，也不需要运行调试命令。完整安装程序已经包含图片解码所需的 Magick.NET 运行库；程序当前面向 x64 Windows，升级器目标为 .NET Framework 4.8。Windows 11 和较新的 Windows 10 已包含 .NET Framework 4.8 或 4.8.1；旧版 Windows 可能需要先安装 4.8。微软的系统兼容表见 [.NET Framework 系统要求](https://learn.microsoft.com/en-us/dotnet/framework/get-started/system-requirements)。

要在线检查和下载更新，该电脑还需要能连接 `api.github.com` 与 `github.com` 的 Release 下载地址。网络代理、防火墙或所在网络屏蔽 GitHub 时，图片查看和编辑本身仍可使用，但在线检查/下载可能失败；此时可从另一台能访问 GitHub 的电脑下载完整安装程序，或手动取得 `.dayouupdate` 包后在更新中心选择它。

新电脑请直接安装 [最新版完整安装程序](https://github.com/dayouAI777/dayou-image-tools/releases/latest)。1.3.1 及更早版本的旧升级器需要完整安装一次 1.3.2 来修复自身；安装 1.3.2 后，后续版本可由更新中心处理。

## 发布新版本

假设新版本是 `1.3.3`：

1. 在 `setup.iss` 更新 `MyAppVersion`、`OutputBaseFilename` 和 `VersionInfoVersion`，三个位置都写成新版本。
2. 在 `AssemblyInfo.cs` 和 `UpdaterAssemblyInfo.cs` 更新 `AssemblyVersion` 与 `AssemblyFileVersion`。
3. 新建 `RELEASE_NOTES_v1.3.3.md`，用简短条目说明修复和新增功能。文件名必须与 `setup.iss` 里的版本一致，否则工作流会在构建前停止。
4. 将代码、版本号和更新说明提交并推送到 `main`。合并到 `main` 的 Pull Request 也会触发工作流。
5. 打开仓库的 **Actions → Build and publish releases**，确认运行结果为绿色成功；然后打开 **Releases**，确认新版本带有安装程序和更新包两个附件。

**不用先去 Releases 页面创建草稿。** 工作流会检查 `v1.3.3` 是否已经发布；如果没有，就自动构建、创建标签和 Release、上传附件并标记为最新版本。同一版本标签已经存在时，工作流会跳过发布，不能靠重复推送覆盖已发布版本；需要发布修正时应提升修订号，例如从 `1.3.3` 改成 `1.3.4`。

## 自动流程具体做什么

工作流依次执行以下步骤：

1. 从 `setup.iss` 读取版本，并要求仓库中存在对应的 `RELEASE_NOTES_vX.Y.Z.md`。
2. 检查该版本是否已经发布；已存在则跳过，避免重复发布。
3. 安装 Python 3.12，运行 `build.cmd` 编译主程序和升级器。
4. 运行 `tools/make_update_package.py --version X.Y.Z`，生成 `.dayouupdate`，并检查 ZIP 成员与 CRC。
5. 在 GitHub Windows runner 上补齐 Inno Setup 的简体中文语言文件，再用 `setup.iss` 构建安装程序。
6. 将发布附件改为 ASCII 英文名，再运行 `gh release create` 上传两份文件，并使用 `--latest` 标记最新版本。

最终附件名为：

- `dayou-image-tools-setup-vX.Y.Z.exe`：首次安装或修复升级器时使用。
- `dayou-image-tools-vX.Y.Z.dayouupdate`：软件内更新中心使用。

附件名用英文，是为了避开 GitHub 上传中文文件名时遇到的问题。Release 标题、说明和程序界面仍可使用中文。

## 曾遇到的问题与修复

- **升级器说 Release 没有 `.dayouupdate`：** .NET Framework 把 GitHub 返回的附件数组反序列化成 `ArrayList`，旧代码只按 `object[]` 读取。1.3.2 已改为兼容集合类型；1.3.1 用户需要先运行一次完整安装程序来更新升级器。
- **Actions 构建时 Python 命令变成空字符串：** `build.cmd` 在括号代码块里设置并引用变量，批处理会过早展开变量。脚本现已启用延迟展开。
- **更新包脚本因中文文件名而退出：** Windows runner 默认代码页不能输出中文；打包步骤设置 `PYTHONIOENCODING=utf-8`。
- **Inno Setup 找不到简体中文语言文件：** runner 的 Inno Setup 安装目录缺少 `ChineseSimplified.isl`；工作流从 Inno Setup 官方源码仓库补齐后再编译。
- **Release 附件名无法上传：** 自动复制为英文 ASCII 文件名后上传。

## 发布失败时

先打开失败运行的 **Actions 日志**，定位第一个失败的步骤。修正源码或工作流后推送新提交，自动发布会再次运行。若失败发生在 Release 创建之前，也可以在 Actions 页面重新运行失败的工作流。若 Release 已创建但附件不全，先检查实际 Release 状态；不要重复创建同一个版本标签。

发布成功后应确认：

- Actions 工作流结论为成功。
- `/releases/latest` 指向新版本，且有 `.exe` 与 `.dayouupdate` 两个附件。
- 更新中心能识别新版本或显示当前已是最新版。

## 相关文件

- 自动构建和上传：`.github/workflows/publish-release.yml`
- 版本与安装程序配置：`setup.iss`
- 主程序版本信息：`AssemblyInfo.cs`
- 升级器版本信息：`UpdaterAssemblyInfo.cs`
- 更新包生成器：`tools/make_update_package.py`
- 更新包结构说明：`UPDATE_PACKAGE_FORMAT.md`


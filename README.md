# 大有图片工具

一个独立的 Windows 图片查看、编辑与批量处理工具。源码是独立编写的 WinForms 程序，不调用 WPS 程序文件或内部接口。

## 启动

安装版：运行 GitHub Releases 提供的最新版安装程序，可选择安装目录，并在安装后将“大有图片工具”加入图片文件的“打开方式”。安装不会改变现有的默认打开程序。

开发启动：双击 `run.cmd`，或把图片/文件夹拖入窗口。也可以传入路径：

```bat
run.cmd "D:\Pictures\photo.jpg"
run.cmd "D:\Pictures"
```

## 查看

- 文件夹缩略图、多选、上一张/下一张、幻灯片和全屏
- 鼠标滚轮以指针位置为锚点缩放；方向键切换图片
- 默认按文件日期从新到旧排序，可在工具栏切换拍摄日期、文件名和文件大小，并选择顺序
- 放大缩小、适应窗口、原始大小、拖动画布、左右旋转
- 复制到剪贴板、设为桌面壁纸、打印、另存副本、在资源管理器中定位
- 查看图片尺寸、文件大小、时间和可读取的 EXIF 信息
- 导出 GIF/TIFF 等多帧图像的帧为 PNG

### 图片格式

除 Windows 自带解码器支持的 BMP、JPEG、PNG、GIF、TIFF、ICO、WMF、EMF 等格式外，安装包还携带 Magick.NET / ImageMagick 解码引擎，扩展支持 WebP、AVIF、HEIC/HEIF、JPEG XL、JPEG 2000、PSD/PSB、XCF、DDS、TGA、OpenEXR、Radiance HDR、Netpbm、QOI、SVG，以及多种相机 RAW 扩展名。查看、缩略图、编辑和批处理共用解码入口；打不开时会显示解码错误。

像 PDF、PostScript/EPS、部分 RAW 和专有格式可能需要外部 delegate 或特定型号的解码器；这类格式受打包的 ImageMagick 构建能力限制，不能保证每个变体都可读。动画与多页文件通常以系统解码器提供的帧为准；扩展解码器回退时先显示首帧。

## 编辑

“编辑”会打开单独的编辑窗口，提供撤销/重做、框选裁剪、左右旋转、翻转、纠偏、改尺寸、扩展画布、画笔、文字、箭头、矩形标注、马赛克、文字/图片水印、边框、滤镜、亮度/对比度/饱和度调整、锐化和柔化。默认另存为新文件，避免覆盖原图。

裁剪页现在提供自由、原图、1:1、4:3、3:2、16:9、9:16、3:4、2:3、5:4、4:5 等比例；也可以输入自定义输出宽高（像素），拖选区域会跟随该宽高比例，应用后输出指定尺寸。

## 批处理

可批量改尺寸、居中裁剪、压缩 JPEG、格式转换、旋转、重命名、加文字水印、复制、移动，或把文件移入 Windows 回收站。除“移动”和“移入回收站”外，处理结果写入输出文件夹；移入回收站前会再次确认。

## 升级

开始菜单提供“升级大有图片工具”。从 1.3.0 起，升级程序可通过公开 GitHub Releases 检查新版、显示更新说明、下载 `.dayouupdate` 并校验 GitHub 提供的 SHA-256 摘要；确认后再由本地部署器校验包内文件清单、备份旧文件并更新，失败时自动回滚。1.3.1 修正了独立升级器的 .NET Framework TLS 目标配置并增加网络诊断；1.3.2 修正了 GitHub Release 附件列表的解析兼容性。也可以继续选择本地 `.dayouupdate` 或 ZIP 包。初次获得在线更新功能，请先安装 Releases 中最新版本的完整安装程序；此后日常版本可从更新中心在线更新。升级器自身仍通过完整安装程序升级。更新包制作脚本和规范见 `tools\make_update_package.py` 与 `UPDATE_PACKAGE_FORMAT.md`。

### 发布在线更新

在 GitHub 上创建 `v主版本.次版本.修订号` 格式的正式 Release，并上传同版本 `.dayouupdate` 文件。桌面升级器从 `dayouAI777/dayou-image-tools` 的最新正式 Release 读取版本和更新说明；只有资产名以 `.dayouupdate` 结尾的包才会提供给程序下载。首次安装请使用 Release 中的完整安装程序。

## 范围与限制

- 这是一个覆盖常用本地查看、编辑和整理流程的工作台，不是 WPS 图片的一比一替代品。
- OCR、AI 抠图/去水印/画质增强/扩图、人像美化、在线素材等依赖模型或云端服务的能力没有接入。
- SVG、RAW、HEIC、AVIF 等扩展格式由随程序安装的解码引擎尝试读取；依赖外部 delegate 的文件可能仍无法打开。
- 批量裁剪固定取图片中央；图片转 PDF 可在打印对话框中选择系统的“Microsoft Print to PDF”。
- GIF/动画图像另存单张时保存当前帧；批量编辑按解码器提供的首帧处理。

## 构建

双击 `build.cmd` 生成 x64 主程序和升级程序。如果 `third_party\Magick.NET` 中缺少运行库，构建脚本会从 NuGet 下载固定版本 15.0.0 并恢复所需 DLL。安装包由 Inno Setup 7 使用 `setup.iss` 生成。更新包可在构建后运行 `python tools\make_update_package.py --version 1.3.2`，输出到 `bin\大有图片工具_v1.3.2.dayouupdate`。构建使用 Windows 自带的 .NET Framework C# 编译器、WinForms、`System.Drawing` 和第三方 Magick.NET 解码库。第三方许可文本随安装包放在 `licenses` 文件夹中。

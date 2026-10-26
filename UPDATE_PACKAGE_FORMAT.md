# 大有图片工具更新包格式

升级程序接收 `.dayouupdate` 或 `.zip` 文件。它会识别 ZIP 根目录里的 `dayou-update.xml`，从清单读取版本、包内文件位置和安装目标位置。没有此清单或产品标记不符的压缩包会被拒绝。

## 文件布局

```text
dayou-image-tools-1.3.1.dayouupdate
├── dayou-update.xml
└── payload/
    ├── DayouImageTools.exe
    ├── Magick.NET-Q8-x64.dll
    ├── Magick.NET.Core.dll
    └── Magick.Native-Q8-x64.dll
```

扩展名只是便于识别；`.dayouupdate` 文件实际采用 ZIP 格式。更新清单只部署其中明确列出的文件，安装目标路径相对于已安装程序目录，因此可以更新根目录文件或子目录文件。

## 清单示例

```xml
<?xml version="1.0" encoding="utf-8"?>
<dayouUpdate product="DayouImageTools" version="1.3.1" minimumUpdaterVersion="1.3.0">
  <file source="payload/DayouImageTools.exe" target="DayouImageTools.exe" sha256="替换为64位十六进制SHA-256" />
  <file source="payload/Magick.NET-Q8-x64.dll" target="Magick.NET-Q8-x64.dll" sha256="替换为64位十六进制SHA-256" />
  <file source="payload/Magick.NET.Core.dll" target="Magick.NET.Core.dll" sha256="替换为64位十六进制SHA-256" />
  <file source="payload/Magick.Native-Q8-x64.dll" target="Magick.Native-Q8-x64.dll" sha256="替换为64位十六进制SHA-256" />
</dayouUpdate>
```

- `product` 必须是 `DayouImageTools`。
- `version` 是更新后的程序版本；不能低于已安装版本。
- `minimumUpdaterVersion` 可选，用来要求较新的升级程序。
- 每个 `file` 都要有 ZIP 内的 `source`、安装目录内的相对 `target` 和对应文件的 SHA-256。
- 包内文件路径和目标路径都不能使用绝对路径或 `..`。升级程序会校验文件数量、解压大小与 SHA-256，不会解压清单未列出的文件。
- 更新前会请求主程序正常关闭，把即将替换的文件备份到 `%LOCALAPPDATA%\大有图片工具\UpdateBackups`；部署失败时自动还原。成功后启动主程序。
- 当前版本的升级程序不能替换自身。如需升级升级程序，请运行新的完整安装程序。
- 在线更新从 `dayouAI777/dayou-image-tools` 的最新正式 GitHub Release 检查；Release 标签必须是 `v1.2.3` 这种版本号，并且要附带 `.dayouupdate` 资产。升级器会验证下载地址为 GitHub HTTPS 域名、资产大小、GitHub 提供的 SHA-256 摘要，以及包内逐文件 SHA-256。

下次制作更新时，先构建程序，再运行 `python tools/make_update_package.py --version 1.3.1` 自动复制当前程序文件、生成含 SHA-256 的清单并打成 `.dayouupdate`。将该包上传到同版本的 GitHub Release。用户打开“升级大有图片工具”，检查、下载并确认后即可部署。

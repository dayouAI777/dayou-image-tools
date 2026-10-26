# 大有图片工具 1.3.2

## 修复

- 修复 GitHub 返回的 Release 附件列表被 .NET Framework 解析为 `ArrayList` 时，升级器误报没有 `.dayouupdate` 更新包的问题。
- 保留 1.3.1 的 .NET Framework 4.8 TLS 默认配置和网络错误诊断。

## 升级说明

- 1.3.1 用户请运行本版完整安装程序一次，以更新升级程序本身。
- 后续版本可继续通过 GitHub Releases 发布 `.dayouupdate` 更新包。

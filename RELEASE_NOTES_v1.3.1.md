# 大有图片工具 1.3.1

## 修复

- 修复升级程序在部分 Windows/.NET 配置下无法连接 GitHub Releases 的问题：明确按 .NET Framework 4.8 目标运行，并使用操作系统安全 TLS 默认值。
- 在线检查失败时显示网络错误类型和具体原因，便于排查代理、DNS 或 TLS 问题。

## 升级说明

- 1.3.0 用户请运行本版完整安装程序一次，以更新升级程序本身。
- 后续版本可继续在 GitHub Releases 上传 `.dayouupdate` 更新包。

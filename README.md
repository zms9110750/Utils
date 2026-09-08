# Utils

这是一个由 **zmsTemplate** 生成的开源项目。

---

## 特性

### 编译自动格式化

项目根目录有 `.editorconfig`，每次 `dotnet build` 前自动执行 `dotnet format`。代码风格统一，无需手动整理。

### GitHub 工作流

`.github/workflows/ci.yml` 包含了完整的 CI/CD：

- **PR 到 main/master** — 自动 `dotnet restore` → `build` → `test`，测试通过才能合并
- **推送 `v*` 标签** — 自动打包发版

### 集中配置

所有项目的版本号、作者、仓库地址统一写在 `Directory.Build.props` 中。修改版本只需改这一个文件。

### 解决方案结构

解决方案已按文件夹组织：

- `/src/` — 类库
- `/test/` — 测试项目

---

## 项目说明



### 类库

生成 XML 文档文件，裸用 DLL 也能看到注释提示。

---


---

## 开发流程

### 分支策略

```
main          ← 稳定分支，PR 合并目标
  └─ feature/xxx  ← 功能分支，from main 分出
```

所有改动在功能分支上进行，完成后提交 Pull Request 到 `main`。

- 分支命名：`feature/简短描述` 或 `fix/简短描述`
- PR 标题：清晰说明改动内容
- 合并方式：**Squash merge**（将分支上所有提交压缩为一个提交）

### 发版

推送 `v*` 标签（如 `v0.1.0`）时，GitHub Actions 自动：

1. 编译 + 测试
2. 打 nupkg
3. 创建 GitHub Release
4. Release Notes 根据 PR 标签自动分类生成

标签名即版本号，与 `Directory.Build.props` 中的 `<Version>` 保持一致。

**推送标签：** 在项目目录执行以下命令，后续 `git push` 会自动携带标签：

```bash
git config push.followTags true
git push
```

首次推送标签可用 `git push --tags`。

---

## 分发说明

GitHub Actions CI 在 `v*` 标签推送时自动构建并发布以下产物：

| 类型 | 说明 |
|------|------|
| **自包含 zip** | 6 个 RID（win-x64/arm64, linux-x64/arm64, osx-x64/arm64），基于最高 TFM 发布 |
| **FDD zip** | 按 TFM 分组，同一 TFM 的多个 exe 合并到同一 zip |
| **NuGet** | 类库项目的 `.nupkg` 包 |

### 限制

- **.NET Framework**（net472/net48 等）：CI 运行在 Linux runner 上，不支持发布基于 Framework 的项目。如果你的项目必须发布 .NET Framework 版本，请在 Windows runner 上自行构建
- **自包含发布的目标框架**：CI 的 `Get-Highest` 函数自动选择项目中的最高 TFM（如 net6.0;net8.0;net9.0 选 net9.0）。如需发布特定 TFM 的自包含包，请调整 `TargetFrameworks` 或手动构建
- **Linux x86-32**：.NET 6 起已移除对 32 位 Linux 的官方支持，本 CI 不提供 `linux-x86` RID
- **预览版目标框架**：CI 默认安装当年 GA 版的 .NET SDK。若你的项目使用了尚未正式发布的 TFM（如 net11.0 在 2026 年），会导致编译/打包失败。请等待 SDK GA 或手动指定 `dotnet-version`

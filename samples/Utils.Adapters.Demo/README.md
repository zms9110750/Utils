# Utils.Adapters.Demo

此项目用于演示 `Utils.Adapters` 中各适配器的用法。

## 项目结构

每个适配器对应一个独立入口文件和素材文件夹：

```
Utils.Adapters.Demo/
├── README.md                  ← 本文件
├── polly.cs                   ← Polly 弹性管道的演示入口
├── FusionCache/               ← (预留) FusionCache 演示
├── *其他适配器.cs              ← (预留) 更多入口
└── *其他适配器文件夹/           ← (预留) 对应素材
```

## 入口文件命名规则

- 每个 `.cs` 入口文件位于项目根目录
- 文件名即入口名，例如 `polly.cs` 对应 `polly`
- 通过 SmallSharp 支持多入口，运行方式：

```
dotnet run -- polly
```

每个入口的素材文件（JSON、配置等）放在同名的子文件夹中，由 `CopyToOutputDirectory` 自动复制到输出目录。

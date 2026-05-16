# 本轮发现的 Issues

> 每行一个发现 = 一个 GitHub Issue。先在 GitHub 建 Issue（用对应模板），再把编号填到这里。

| # | UC 号 | 严重度 | 简述 | Issue |
|---|-------|--------|------|-------|
| 1 | UC-B4 | 🔴 Blocker | (e.g. OpenMOC ScaleNuSigmaF 报 NPE) | #__ |
| 2 | UC-E3 | 🟡 Major  | (e.g. PDF 导出文字乱码) | #__ |
| 3 | UC-A6 | 🟢 Minor  | (e.g. 搜索框响应 ~800ms 略慢) | #__ |
|   | (按需追加) | | | |

## 严重度分类（与 acceptance-rubric 对齐）

- 🔴 **Blocker** — 阻断 Release，必须修
- 🟡 **Major**   — 应修；累计 ≥ 5 个阻断 Release
- 🟢 **Minor**   — 可延期；不影响 Release

## Label 约定

GitHub Issue 必须加以下 label 之一：

- `uat-blocker`
- `uat-major`
- `uat-minor`
- `uat-doc`         (文档错误)
- `uat-env`         (环境装不通)
- `uat-enhancement` (建议 / 新覆盖点)

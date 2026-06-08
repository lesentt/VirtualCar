# 提交到本地 main 分支

将当前工作区改动提交到**本地** `main` 分支。不要 push，除非用户另行要求。

## 执行步骤

1. **并行检查状态**（Shell）：
   - `git status`
   - `git diff` 与 `git diff --cached`
   - `git log --oneline -8`
   - `git branch --show-current`

2. **确认分支**
   - 若不在 `main`，先 `git checkout main`（有未提交改动时先 stash 或让用户确认，勿丢改动）
   - 若无改动可提交，直接告知用户并停止

3. **分析改动并写提交说明**
   - 只看将纳入本次提交的变更
   - 用中文 1–2 句，说明「为什么」，风格与近期 `git log` 一致
   - 不要提交 `.env`、密钥等敏感文件

4. **提交**（顺序执行）
   - `git add` 相关文件
   - 用完整提交说明创建 commit
   - 再跑 `git status` 确认成功

5. **PowerShell 提交说明示例**

```powershell
git commit -m @"
这里是提交说明。

第二行可选。
"@
```

## 禁止

- 不要 `git push`（除非用户明确要求）
- 不要改 `git config`
- 不要 `--no-verify`、force push、`git commit --amend`（除非用户规则允许的情况）
- 不要创建空 commit

## 完成后

简要回复：commit hash、提交说明、是否 working tree clean；若比 `origin/main` 超前，说明仅本地提交、未推送。

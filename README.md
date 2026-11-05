# Ofd2Pdf

Convert OFD files to PDF files.

![Ofd2Pdf GUI](screenshot.png)

你可以单击打开 GUI，选择文件或者拖拽文件到列表，亦或者简单将文件拖拽到可执行文件上，它将自动完成转换过程。

## 批量转换文件夹

- 点击「选择文件夹」，或直接把文件夹拖拽到列表 / 可执行文件上，程序会**递归扫描**其中所有 `.ofd` 文件并加入列表。
- 生成的 PDF 与原 OFD 文件**位于同一目录、同名**，只是扩展名变为 `.pdf`，目录结构保持不变。
- 命令行同样支持文件夹：`Ofd2Pdf.exe D:\发票 D:\其他\a.ofd`

## 下载

在 [Releases](../../releases) 页面下载 `Ofd2Pdf.zip`，解压后运行 `Ofd2Pdf.exe`（需要 .NET Framework 4.8，Windows 10/11 已自带）。

发布新版本：推送 `v` 开头的 tag 即可，GitHub Actions 会自动构建并创建 Release：

```bash
git tag v1.3.0
git push origin v1.3.0
```

using System;
using Xunit;

namespace XUnitTest.Helpers;

/// <summary>需要 API Key 的集成测试 Theory 特性。未检测到有效密钥时自动 Skip，避免因环境未配置导致失败</summary>
/// <remarks>
/// 检测顺序与 <see cref="RequiresApiKeyFactAttribute"/> 一致：
/// 1) 显式文件路径（可相对路径）
/// 2) 主环境变量
/// 3) 备用环境变量
/// </remarks>
public sealed class RequiresApiKeyTheoryAttribute : TheoryAttribute
{
    /// <summary>创建 RequiresApiKeyTheory 特性</summary>
    /// <param name="environmentVariable">主环境变量，如 DASHSCOPE_API_KEY</param>
    /// <param name="filePath">可选密钥文件路径，如 config/DashScope.key（纯文本）</param>
    /// <param name="fallbackEnvironmentVariables">可选备用环境变量</param>
    public RequiresApiKeyTheoryAttribute(String environmentVariable, String? filePath = null, params String[] fallbackEnvironmentVariables)
    {
        if (RequiresApiKeyFactAttribute.HasKey(environmentVariable, filePath, fallbackEnvironmentVariables)) return;

        var source = String.IsNullOrEmpty(filePath)
            ? environmentVariable
            : $"{filePath} 或 {environmentVariable}";
        Skip = $"未检测到可用 API Key（{source}），跳过集成测试";
    }
}

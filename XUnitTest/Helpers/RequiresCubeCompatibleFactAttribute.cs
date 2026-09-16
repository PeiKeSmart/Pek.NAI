using System;
using Xunit;

namespace XUnitTest.Helpers;

/// <summary>需要 Cube 与 NCode 版本兼容的 Fact 特性。版本不匹配时自动 Skip</summary>
/// <remarks>
/// DH.NCube.Core 4.22（2026-05 发布）按当时含 Tenant 属性的 XCode 编译，其 TenantMiddleware 调用
/// IManageProvider.set_Tenant；而 DH.NCode 4.25 已移除该成员，导致 MissingMethodException。
/// 待 DH.NCube.Core 4.25+ 重新编译发布后，条件满足将自动恢复运行，无需修改测试代码。
/// </remarks>
public sealed class RequiresCubeCompatibleFactAttribute : FactAttribute
{
    private static Boolean? _compatible;

    public RequiresCubeCompatibleFactAttribute()
    {
        if (!IsCubeCompatible(out var version))
            Skip = $"DH.NCube.Core 与 DH.NCode 版本不兼容（当前 Cube {version}，需要 4.25+），待新版本发布后自动恢复";
    }

    internal static Boolean IsCubeCompatible(out String? version)
    {
        var asm = typeof(NewLife.Cube.CubeService).Assembly;
        var v = asm.GetName().Version;
        version = v?.ToString();

        // DH.NCube.Core 4.25+ 为适配 DH.NCode 4.25 重新编译的版本
        return v != null && (v.Major > 4 || (v.Major == 4 && v.Minor >= 25));
    }
}

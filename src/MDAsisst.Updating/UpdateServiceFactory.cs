using MDAsisst.Core.Logging;
using MDAsisst.Core.Settings;

namespace MDAsisst.Updating;

/// <summary>更新モードに応じて実装を選ぶ。Disabled では通信可能な実装を生成しない。</summary>
public static class UpdateServiceFactory
{
    public static IUpdateService Create(UpdateMode mode, ILogSink? log = null)
        => mode == UpdateMode.Disabled
            ? new NullUpdateService()
            : new VelopackUpdateService(log);
}

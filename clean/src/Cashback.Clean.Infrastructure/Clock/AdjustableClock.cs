using System.Globalization;

namespace Cashback.Clean.Infrastructure.Clock;

/// <summary>
/// Relógio da demonstração (aula 3): a hora real mais um ajuste guardado num arquivo. Em sala, serve para "pular"
/// para a sexta-feira 00h da Black Friday sem esperar. O ajuste fica num arquivo, e não na memória, para que as
/// duas versões da API no ar durante o canário (dois processos) vejam a mesma hora.
/// Só existe quando a configuração Demo:ClockFile aponta um arquivo (o rodar-apis.sh faz isso); senão, o app usa o relógio do sistema.
/// </summary>
public class AdjustableClock(string file) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => base.GetUtcNow() + Offset();

    public bool IsAdjusted => File.Exists(file);

    public void Set(DateTimeOffset now) =>
        File.WriteAllText(file, (now - base.GetUtcNow()).Ticks.ToString(CultureInfo.InvariantCulture));

    public void Reset() => File.Delete(file);

    private TimeSpan Offset()
    {
        try
        {
            return File.Exists(file) ? TimeSpan.FromTicks(long.Parse(File.ReadAllText(file), CultureInfo.InvariantCulture)) : TimeSpan.Zero;
        }
        catch (Exception e) when (e is IOException or FormatException)
        {
            return TimeSpan.Zero;
        }
    }
}

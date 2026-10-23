namespace Cashback.Clean.Domain.Rules;

/// <summary>
/// Campanha de Black Friday (aula 3): +5% em todas as lojas, menos combustível.
/// Vai para produção desligada, atrás da feature flag "black-friday". O horário da campanha
/// (sexta 27/11 00h00 a domingo 29/11 23h59) fica na flag, não aqui: quem liga é o relógio, não um deploy.
/// </summary>
public class BlackFridayCampaign : ICashbackRule
{
    public string? Flag => "black-friday";

    // Correção de sábado: a farmácia tinha 10% aqui, somando de novo os 5% que a PharmacyCampaign já dá.
    // A Black Friday soma 5% em toda categoria, menos combustível; as outras campanhas continuam valendo por conta própria.
    public decimal PercentageFor(string category, DateTimeOffset purchaseDate) => category == "fuel" ? 0m : 0.05m;
}

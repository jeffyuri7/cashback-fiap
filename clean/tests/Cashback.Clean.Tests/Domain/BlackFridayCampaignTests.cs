using Cashback.Clean.Domain.Rules;

namespace Cashback.Clean.Tests.Domain;

// A campanha da Black Friday (aula 3): +5% em todas as lojas, menos combustível, e só com a flag ligada.
public class BlackFridayCampaignTests
{
    private static readonly DateTimeOffset BlackFriday = new(2026, 11, 27, 12, 0, 0, TimeSpan.FromHours(-3));
    private static readonly HashSet<string> FlagOn = ["black-friday"];

    private static CashbackCalculator CreateCalculator() =>
        new([new CategoryRule(), new SupermarketDoubleCampaign(), new BlackFridayCampaign()]);

    [Fact]
    public void Supermarket_gets_5_percent_more_with_the_flag_on()
    {
        var quote = CreateCalculator().Calculate(100m, "supermarket", BlackFriday, FlagOn);

        Assert.Equal(0.09m, quote.Percentage); // 2% + 2% (supermercado em dobro) + 5% (Black Friday)
    }

    [Fact]
    public void Fuel_is_out_of_the_campaign()
    {
        var quote = CreateCalculator().Calculate(100m, "fuel", BlackFriday, FlagOn);

        Assert.Equal(0.03m, quote.Percentage);
    }

    // A correção de sábado: a farmácia pagava 10% a mais, e nenhum teste olhava a farmácia.
    // Agora todas as categorias são cobertas: a campanha soma exatamente 5%, menos no combustível.
    [Theory]
    [InlineData("supermarket", 0.05)]
    [InlineData("pharmacy", 0.05)]
    [InlineData("books", 0.05)]
    [InlineData("fuel", 0.0)]
    public void The_campaign_adds_exactly_5_percent_except_fuel(string category, double expected)
    {
        var calculator = new CashbackCalculator(
            [new CategoryRule(), new SupermarketDoubleCampaign(), new PharmacyCampaign(), new BlackFridayCampaign()]);

        var withFlag = calculator.Calculate(100m, category, BlackFriday, FlagOn);
        var withoutFlag = calculator.Calculate(100m, category, BlackFriday);

        Assert.Equal((decimal)expected, withFlag.Percentage - withoutFlag.Percentage);
        Assert.Equal(withFlag.Amount - withoutFlag.Amount, withFlag.CampaignBonus);
    }

    [Fact]
    public void With_the_flag_off_the_campaign_does_not_count()
    {
        var quote = CreateCalculator().Calculate(100m, "supermarket", BlackFriday);

        Assert.Equal(0.04m, quote.Percentage);
    }
}

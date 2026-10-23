using Cashback.Clean.Domain.Rules;

namespace Cashback.Clean.Tests.Domain;

// A rede de proteção contra o defeito do big ball of mud: uma regra que guarda estado entre compras.
// Na API, as regras e a calculadora são singleton (um objeto para a API inteira). Se uma regra guardar
// um valor de uma compra para a outra, todos os clientes passam a ser afetados. Estes testes usam o
// MESMO objeto em várias compras seguidas, como acontece em produção, e cobrem sozinhos qualquer regra
// nova (encontram todas as classes que implementam ICashbackRule).
public class RulesAreStatelessTests
{
    private static readonly DateTimeOffset October = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly string[] Categories = ["supermarket", "fuel", "pharmacy", "books"];

    // todas as regras de cashback que existem no projeto, inclusive as que forem criadas depois
    private static IEnumerable<Type> RuleTypes() =>
        typeof(ICashbackRule).Assembly.GetTypes()
            .Where(t => typeof(ICashbackRule).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .OrderBy(t => t.Name);

    private static ICashbackRule NewRule(Type type) => (ICashbackRule)Activator.CreateInstance(type)!;

    private static CashbackCalculator NewCalculator() => new(RuleTypes().Select(NewRule).ToList());

    [Fact]
    public void A_purchase_does_not_change_the_percentage_of_the_next_one()
    {
        // a mesma calculadora atende uma compra depois da outra, como na API
        var calculator = NewCalculator();
        var supermarketAlone = NewCalculator().Calculate(250m, "supermarket", October).Percentage;

        calculator.Calculate(100m, "pharmacy", October);                          // Ana compra na farmácia
        var supermarket = calculator.Calculate(250m, "supermarket", October);    // logo depois, Bruno no mercado

        Assert.True(supermarket.Percentage == supermarketAlone,
            $"O Bruno ganhou {supermarket.Percentage:P0} no mercado depois de uma compra na farmácia, " +
            $"mas o certo é {supermarketAlone:P0}: alguma regra guardou estado da compra anterior " +
            "(o mesmo defeito do big ball of mud).");
    }

    [Fact]
    public void No_rule_keeps_state_between_purchases()
    {
        var problems = new List<string>();
        foreach (var type in RuleTypes())
        {
            // a mesma instância passa por todas as categorias, nas duas ordens…
            var shared = NewRule(type);
            foreach (var category in Categories.Concat(Categories.Reverse()))
                shared.PercentageFor(category, October);

            // …e tem que responder igual a uma instância nova, que nunca calculou nada
            foreach (var category in Categories)
            {
                var expected = NewRule(type).PercentageFor(category, October);
                var actual = shared.PercentageFor(category, October);
                if (actual != expected)
                    problems.Add($"{type.Name} deu {actual:P0} para \"{category}\" depois de outras compras " +
                                 $"(numa instância nova dá {expected:P0})");
            }
        }

        Assert.True(problems.Count == 0,
            "Regra que guarda estado entre compras (o mesmo defeito do big ball of mud):\n- " +
            string.Join("\n- ", problems));
    }
}

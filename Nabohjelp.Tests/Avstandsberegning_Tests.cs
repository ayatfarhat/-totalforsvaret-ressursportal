using Nabohjelp.Services;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for avstanden i luftlinje mellom et behov og en ressurs (Services/Avstandsberegning.cs).
public class AvstandsberegningTests
{
    [Fact]
    public void SammePosisjon_GirNullKilometer()
    {
        double? avstand = Avstandsberegning.BeregnAvstandKm(63.4305, 10.3951, 63.4305m, 10.3951m);

        Assert.NotNull(avstand);
        Assert.Equal(0, avstand!.Value, 5);
    }

    [Fact]
    public void TrondheimTilOslo_GirOmtrent390Kilometer()
    {
        double? avstand = Avstandsberegning.BeregnAvstandKm(63.4305, 10.3951, 59.9139m, 10.7522m);

        Assert.NotNull(avstand);
        Assert.InRange(avstand!.Value, 390, 393);
    }

    [Theory]
    [InlineData(null, null)]   // ressursen har ingen posisjon
    [InlineData(95.0, 10.0)]   // breddegrad utenfor jordkloden
    [InlineData(0.0, 0.0)]     // 0,0 betyr at ingen posisjon ble valgt
    public void UgyldigPosisjon_GirIngenAvstand(double? bredde, double? lengde)
    {
        // InlineData kan ikke inneholde decimal, så verdiene gjøres om her.
        double? avstand = Avstandsberegning.BeregnAvstandKm(63.4305, 10.3951, (decimal?)bredde, (decimal?)lengde);

        Assert.Null(avstand);
    }
}

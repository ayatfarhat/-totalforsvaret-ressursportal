using Nabohjelp.Services;
using Xunit;

namespace Nabohjelp.Tests;

public class Avstandsberegning_Tests
{
    [Fact]
    public void SammePosisjon_GirNullKilometer()
    {
        var avstand = Avstandsberegning.BeregnAvstandKm(
            63.4305, 10.3951,
            63.4305m, 10.3951m);

        Assert.NotNull(avstand);
        Assert.Equal(0, avstand.Value, 5);
    }

    [Fact]
    public void TrondheimTilOslo_GirOmtrent390Kilometer()
    {
        var avstand = Avstandsberegning.BeregnAvstandKm(
            63.4305, 10.3951,
            59.9139m, 10.7522m);

        Assert.NotNull(avstand);
        Assert.InRange(avstand.Value, 390, 393);
    }

    [Fact]
    public void ManglendeKoordinater_GirNull()
    {
        var avstand = Avstandsberegning.BeregnAvstandKm(
            63.4305, 10.3951,
            null, null);

        Assert.Null(avstand);
    }

    [Fact]
    public void UgyldigBreddegrad_GirNull()
    {
        var avstand = Avstandsberegning.BeregnAvstandKm(
            63.4305, 10.3951,
            95m, 10.3951m);

        Assert.Null(avstand);
    }

    [Fact]
    public void NullNullKoordinater_GirNull()
    {
        var avstand = Avstandsberegning.BeregnAvstandKm(
            63.4305, 10.3951,
            0m, 0m);

        Assert.Null(avstand);
    }
}
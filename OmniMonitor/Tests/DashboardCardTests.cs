using ApexCharts;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using OmniMonitor.Client.Shared;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;

public class DashboardCardTests : TestContext
{
    public DashboardCardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Mock para IStringLocalizer<DashboardCard>
        var localizerMock = new Mock<IStringLocalizer<DashboardCard>>();
        Services.AddSingleton(localizerMock.Object);
    }


    [Fact]
    public void RendersKpiTitleCorrectly() {
        // Arrange & Act
        var cut = RenderComponent<DashboardCard>(
            ComponentParameter.CreateParameter(nameof(DashboardCard.esKPI), true),
            ComponentParameter.CreateParameter(nameof(DashboardCard.Title), "Mi KPI")
        );
        // Assert
        Assert.Contains("Mi KPI", cut.Markup);
    }

    [Fact]
    public void Kpi_RendersStaticValues()
    {
        var cut = RenderComponent<DashboardCard>(
            ComponentParameter.CreateParameter(nameof(DashboardCard.esKPI), true)
        );

        var markup = cut.Markup;
        Assert.Contains("2,405", markup);
        Assert.Contains("último mes", markup);
    }

    [Fact]
    public void Graph_HasCorrectSeriesData()
    {
        var data = new (string, string, double, double[])[]
        {
        ("Sales", "#ff0000", 0, new double[] { 10, 20, 30 }),
        ("Costs", "#00ff00", 0, new double[] { 5, 10, 15 })
        };

        var cut = RenderComponent<DashboardCard>(
            ComponentParameter.CreateParameter(nameof(DashboardCard.esKPI), false),
            ComponentParameter.CreateParameter(nameof(DashboardCard.Datos), data)
        );

        // Verificamos cantidad de series
        Assert.Equal(2, cut.Instance.Datos.Length);

        // Verificamos nombres y colores
        Assert.Equal("Sales", cut.Instance.Datos[0].Item1);
        Assert.Equal("#ff0000", cut.Instance.Datos[0].Item2);
        Assert.Equal("Costs", cut.Instance.Datos[1].Item1);
        Assert.Equal("#00ff00", cut.Instance.Datos[1].Item2);

        // Verificamos longitud de datos
        Assert.Equal(3, cut.Instance.Datos[0].Item4.Length);
        Assert.Equal(3, cut.Instance.Datos[1].Item4.Length);

        // Verificamos algunos valores
        Assert.Equal(10, cut.Instance.Datos[0].Item4[0]);
        Assert.Equal(15, cut.Instance.Datos[1].Item4[2]);
    }

    [Fact]
    public void Graph_ResizeUp_DecreasesHeight()
    {
        var cut = RenderComponent<DashboardCard>(
            ComponentParameter.CreateParameter(nameof(DashboardCard.esKPI), false),
            ComponentParameter.CreateParameter(nameof(DashboardCard.height), 400)
        );

        var buttons = cut.FindAll("button.mud-icon-button");
        buttons[0].Click(); // Up arrow

        Assert.Equal(350, cut.Instance.height);
    }

    [Fact]
    public void Graph_ResizeUp_DecreasesHeight_MinHeight()
    {
        var cut = RenderComponent<DashboardCard>(
            ComponentParameter.CreateParameter(nameof(DashboardCard.esKPI), false),
            ComponentParameter.CreateParameter(nameof(DashboardCard.height), 300)
        );

        var buttons = cut.FindAll("button.mud-icon-button");
        buttons[0].Click(); // Up arrow

        Assert.Equal(300, cut.Instance.height);
    }

    [Fact]
    public void Graph_ResizeDown_IncreasesHeight()
    {
        var cut = RenderComponent<DashboardCard>(
            ComponentParameter.CreateParameter(nameof(DashboardCard.esKPI), false),
            ComponentParameter.CreateParameter(nameof(DashboardCard.height), 400)
        );

        var buttons = cut.FindAll("button.mud-icon-button");
        buttons[1].Click(); // Down arrow

        Assert.Equal(450, cut.Instance.height);
    }

    [Fact]
    public void Graph_ResizeLeft_DecreasesWidth_LgBreakpoint()
    {
        var cut = RenderComponent<CascadingValue<Breakpoint>>(parameters => parameters
        .Add(p => p.Value, Breakpoint.Lg)
        .AddChildContent<DashboardCard>(child => child
        .Add(p => p.esKPI, false)
        .Add(p => p.WidthLg, 6)
    )
);

        var buttons = cut.FindAll("button.mud-icon-button");
        buttons[2].Click(); // Left arrow
        Assert.Equal(5, cut.FindComponent<DashboardCard>().Instance.WidthLg);
    }

    [Fact]
    public void Graph_ResizeRight_IncreasesWidth_LgBreakpoint()
    {
        var cut = RenderComponent<CascadingValue<Breakpoint>>(parameters => parameters
            .Add(p => p.Value, Breakpoint.Lg)
            .AddChildContent<DashboardCard>(child => child
                .Add(p => p.esKPI, false)
                .Add(p => p.WidthLg, 4)
            )
        );
        var buttons = cut.FindAll("button.mud-icon-button");
        buttons[3].Click(); // Right arrow
        Assert.Equal(5, cut.FindComponent<DashboardCard>().Instance.WidthLg);
    }

    [Fact]
    public void Graph_ResizeLeft_DecreasesWidth_MdBreakpoint()
    {
        var cut = RenderComponent<CascadingValue<Breakpoint>>(parameters => parameters
            .Add(p => p.Value, Breakpoint.Md)
            .AddChildContent<DashboardCard>(child => child
                .Add(p => p.esKPI, false)
                .Add(p => p.WidthMd, 6)
            )
        );
        var buttons = cut.FindAll("button.mud-icon-button");
        buttons[2].Click();
        Assert.Equal(5, cut.FindComponent<DashboardCard>().Instance.WidthMd);
    }

    [Fact]
    public void Graph_ResizeRight_IncreasesWidth_MdBreakpoint()
    {
        var cut = RenderComponent<CascadingValue<Breakpoint>>(parameters => parameters
            .Add(p => p.Value, Breakpoint.Md)
            .AddChildContent<DashboardCard>(child => child
                .Add(p => p.esKPI, false)
                .Add(p => p.WidthMd, 4)
            )
        );
        var buttons = cut.FindAll("button.mud-icon-button");
        buttons[3].Click();
        Assert.Equal(5, cut.FindComponent<DashboardCard>().Instance.WidthMd);
    }

    [Fact]
    public void Graph_ResizeLeft_DecreasesWidth_XsBreakpoint()
    {
        var cut = RenderComponent<CascadingValue<Breakpoint>>(parameters => parameters
            .Add(p => p.Value, Breakpoint.Xs)
            .AddChildContent<DashboardCard>(child => child
                .Add(p => p.esKPI, false)
                .Add(p => p.WidthXs, 7)
            )
        );
        var buttons = cut.FindAll("button.mud-icon-button");
        buttons[2].Click();
        Assert.Equal(6, cut.FindComponent<DashboardCard>().Instance.WidthXs);
    }

    [Fact]
    public void Graph_ResizeRight_IncreasesWidth_XsBreakpoint()
    {
        var cut = RenderComponent<CascadingValue<Breakpoint>>(parameters => parameters
            .Add(p => p.Value, Breakpoint.Xs)
            .AddChildContent<DashboardCard>(child => child
                .Add(p => p.esKPI, false)
                .Add(p => p.WidthXs, 4)
            )
        );
        var buttons = cut.FindAll("button.mud-icon-button");
        buttons[3].Click();
        Assert.Equal(5, cut.FindComponent<DashboardCard>().Instance.WidthXs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TipoGrafica_IndexIsWithinValidRange(int tipoIndex)
    {
        var cut = RenderComponent<DashboardCard>(
            ComponentParameter.CreateParameter(nameof(DashboardCard.esKPI), false),
            ComponentParameter.CreateParameter(nameof(DashboardCard.GraphType), tipoIndex)
        );

        // Verificamos que el índice es válido
        Assert.InRange(cut.Instance.GraphType, 0, 2);
    }
}

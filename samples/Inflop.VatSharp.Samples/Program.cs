using System.Globalization;
using Inflop.VatSharp.Samples.Demos;

Console.OutputEncoding = System.Text.Encoding.UTF8;

CultureInfo.DefaultThreadCurrentCulture   = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

DirectApiDemo.Run();
FluentMappingWithDocumentDemo.Run();
FluentMappingItemsOnlyDemo.Run();
CalculationMethodsDemo.Run();
DiscountsDemo.Run();
ForeignCurrencyDemo.Run();
CustomRoundingDemo.Run();
DependencyInjectionDemo.Run();

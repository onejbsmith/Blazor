#pragma checksum "D:\Source\Repos\Blazor\BlazorTrader\Components\ArcGaugeSample - Copy.razor" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "eafffa4150807838bf369ef21b1b85490246a693"
// <auto-generated/>
#pragma warning disable 1591
#pragma warning disable 0414
#pragma warning disable 0649
#pragma warning disable 0169

namespace BlazorTrader.Components
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
#nullable restore
#line 1 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using System.Net.Http;

#line default
#line hidden
#nullable disable
#nullable restore
#line 2 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using Microsoft.AspNetCore.Authorization;

#line default
#line hidden
#nullable disable
#nullable restore
#line 3 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using Microsoft.AspNetCore.Components.Authorization;

#line default
#line hidden
#nullable disable
#nullable restore
#line 4 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using Microsoft.AspNetCore.Components.Forms;

#line default
#line hidden
#nullable disable
#nullable restore
#line 5 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using Microsoft.AspNetCore.Components.Routing;

#line default
#line hidden
#nullable disable
#nullable restore
#line 6 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using Microsoft.AspNetCore.Components.Web;

#line default
#line hidden
#nullable disable
#nullable restore
#line 7 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using Microsoft.JSInterop;

#line default
#line hidden
#nullable disable
#nullable restore
#line 8 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using BlazorTrader;

#line default
#line hidden
#nullable disable
#nullable restore
#line 9 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using BlazorTrader.Shared;

#line default
#line hidden
#nullable disable
#nullable restore
#line 11 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using Radzen.Blazor;

#line default
#line hidden
#nullable disable
#nullable restore
#line 12 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using BlazorStrap;

#line default
#line hidden
#nullable disable
#nullable restore
#line 13 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using Syncfusion.Blazor;

#line default
#line hidden
#nullable disable
#nullable restore
#line 14 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using Syncfusion.Blazor.Calendars;

#line default
#line hidden
#nullable disable
#nullable restore
#line 15 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using Syncfusion.Blazor.CircularGauge;

#line default
#line hidden
#nullable disable
#nullable restore
#line 16 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using Syncfusion.Blazor.Charts;

#line default
#line hidden
#nullable disable
#nullable restore
#line 17 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using Blazorise;

#line default
#line hidden
#nullable disable
#nullable restore
#line 18 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using Blazorise.Charts;

#line default
#line hidden
#nullable disable
#nullable restore
#line 19 "D:\Source\Repos\Blazor\BlazorTrader\_Imports.razor"
using ChartJs.Blazor;

#line default
#line hidden
#nullable disable
#nullable restore
#line 2 "D:\Source\Repos\Blazor\BlazorTrader\Components\ArcGaugeSample - Copy.razor"
using Radzen;

#line default
#line hidden
#nullable disable
    public partial class ArcGaugeSample___Copy : Microsoft.AspNetCore.Components.ComponentBase
    {
        #pragma warning disable 1998
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
        }
        #pragma warning restore 1998
#nullable restore
#line 34 "D:\Source\Repos\Blazor\BlazorTrader\Components\ArcGaugeSample - Copy.razor"
       
    bool showValue = true;
    double value = 100;
    IEnumerable<GaugeTickPosition> tickPositions = Enum.GetValues(typeof(GaugeTickPosition)).Cast<GaugeTickPosition>();
    GaugeTickPosition tickPosition = GaugeTickPosition.Outside;

#line default
#line hidden
#nullable disable
    }
}
#pragma warning restore 1591

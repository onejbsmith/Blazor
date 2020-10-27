using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace tdaStreamHub.Components
{
    public partial class GoogleStackedColumnChart
    {
        protected override async Task OnInitializedAsync()
        {
            var hierarchies = new Hierarchies();
            var data = hierarchies.data;
            await JsRuntime.InvokeAsync<Task>("drawChart", data, this);

        }


    }

    public class Hierarchy
    {
        public int MemberId { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }
    }

    public class Hierarchies
    {
        public List<Hierarchy> data
        {
            get
            {
                return new List<Hierarchy> {
                     new Hierarchy {  MemberId= 1, Name= "Nancy Davolio", ParentId = 4 },
                     new Hierarchy {  MemberId= 2, Name= "Andrew Fuller", ParentId = 4 },
                     new Hierarchy {  MemberId= 3, Name= "Janet Leverling", ParentId = 4 },
                     new Hierarchy {  MemberId= 4, Name= "Margaret Peacock", ParentId = null },
                     new Hierarchy {  MemberId= 5, Name= "Steven Buchanan", ParentId = 3 },
                     new Hierarchy {  MemberId= 6, Name= "Michael Suyama", ParentId =  3 },
                     new Hierarchy {  MemberId= 7, Name= "Robert King", ParentId = 2 },
                     new Hierarchy {  MemberId= 8, Name= "Laura Callahan", ParentId = 2 },
                     new Hierarchy {  MemberId= 9, Name= "Anne Dodsworth", ParentId = 2 }
                };
            }
        }

    }
}

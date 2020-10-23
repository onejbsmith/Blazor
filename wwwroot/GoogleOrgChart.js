﻿$(document).ready(function () {
    console.log("ready!");

    window.drawChart = (data) => {
        google.load("visualization", "1", { packages: ["orgchart"] });
        google.setOnLoadCallback(drawGoogleChart);
        function drawGoogleChart() {
            var dt = new google.visualization.DataTable();
            dt.addColumn('string', 'Entity');
            dt.addColumn('string', 'ParentEntity');
            dt.addColumn('string', 'ToolTip');
            for (var i = 0; i < data.length;i++) {
                var memberId = data[i].memberId.toString();
                var memberName = data[i].name;
                var parentId = data[i].parentId != null ? data[i].parentId.toString() : '';
                dt.addRows([[{
                    v: memberId
                    //,
                    //f: memberName + '<div><img src="img/' + memberId + '.jpg" /></div>'
                }, parentId, memberName]]);
            }
            var doc = document.getElementById("chart");
            if (doc != null) {
                var chart = new google.visualization.OrgChart(doc);
                chart.draw(dt, { allowHtml: true });
            }
        }
    };
});
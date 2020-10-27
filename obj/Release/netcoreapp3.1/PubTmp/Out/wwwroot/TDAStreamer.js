//alert("Hi!");
//var tdaStreamerWebSocket = new WebSocket("wss://" + userPrincipalsResponse.streamerInfo.streamerSocketUrl + "/ws");

var dotNetObject = {};
//var requestLoginJson = "";
var tdaStreamerWebSocket = {};


window.Initialize = function (dotNetObj) {
    this.dotNetObject = dotNetObj;

};

window.Echo = function (jsonText) {
    dotNetObject.invokeMethodAsync('TDAStreamerOnMessage', jsonText);
};

window.Connect = function () {
    tdaStreamerWebSocket = new WebSocket("wss://streamer-ws.tdameritrade.com/ws");
    dotNetObject.invokeMethodAsync('TDAStreamerStatus', tdaStreamerWebSocket.readyState.toString());
    window.resizeTo(800, 800);

    tdaStreamerWebSocket.onopen = function () {
        /// Call a .Net method passing the status to it
        //if (requestLoginJson.length > 0)
        //    tdaStreamerWebSocket.send(requestLoginJson);
        dotNetObject.invokeMethodAsync('TDAStreamerStatus', tdaStreamerWebSocket.readyState.toString());
    };

    /// Streamed data will come to here
    /// everytime TDA sends a frame
    tdaStreamerWebSocket.onmessage = function (tdaStreamer) {
        /// Call a .Net method passing the received data to it
        dotNetObject.invokeMethodAsync('TDAStreamerOnMessage', tdaStreamer.data);
    };

    tdaStreamerWebSocket.onclose = function () {
        /// Call a .Net method passing the status to it
        dotNetObject.invokeMethodAsync('TDAStreamerStatus',  tdaStreamerWebSocket.readyState.toString());
    };


};

window.tdaSendRequest = function (requestJson) {
    if (tdaStreamerWebSocket.readyState == 1) {
        tdaStreamerWebSocket.send(requestJson);
        dotNetObject.invokeMethodAsync('TDAStreamerOnMessage',"SENT: " + requestJson);
    }
    else {
        window.Connect();
    }
};

window.exampleJsFunctions = {
    showPrompt: function (message) {
        return prompt(message, 'Type anything here');
    }
};

// .Net will call this method once, 
// passing a single request to it 
// containing an array of TDA Streamer Requests, 
// starting with the TDA Login Request
//window.tdaSendRequest = function (requestJson) {
//    debugger;

//    console.log(this.tdaStreamerWebSocket.url);

//    console.log(requestJson);
//};


//var myRequest = JSON.stringify(testRequest);
//console.log(testRequest);
//alert("Bye!");



// Utility
function jsonToQueryString(json) {
    return Object.keys(json).map(function (key) {
        return encodeURIComponent(key) + '=' +
            encodeURIComponent(json[key]);
    }).join('&');
}

var userPrincipalsResponse = {
    "authToken": "UtYqxbSug3vHxkhSAbXSycKzo5QvCjvete8nJmSGkRhbzJO/pvAPvJOGdCNrCESHo5u+sRiThO7bxuI/jtOd7SPzQC5VLZrxNOGRisCakH33M6ENkfAQTcBbNUWRQgwhTAafCBewypUPMndU+iYuDJujU/cZVd9vUnvMaofkl1R/d6eG14S1mFnQtonT9M7sIX00jezYz3JH1yp/Gs1lgWABIgSsVxcW3HpRo5MKFXNcTpXs635A3zvoVW/+PH6GZSMxlmmSKEt3eIpFB830DdQ+/J5uueWl/F+HV6At9X+ESP5JpIUKAzG/4QvsRCbVS2f8MjWcWdyA28/V+txSDE11k8g3PE+JHY6zhnD/+eBkiXUS5OcSwzzI1WeTlusB+XocWtMFoUZdUyzMD+8KFLGorwJ6g6xdx376mTJtDibQWqANCvSQ4wDE7/Nf6BQywfNU9WFXp7BVFvUjDN2Zk16G1GQCgb7qLQ6FSVhbTYwraO8uPaKTRYkjjwkmocfxL0iBFfiV5FFT+PqeAdcMTcWmqDvq/h1ZY2+lg100MQuG4LYrgoVi/JHHvlL/6sguq1/+YNv7+4JJuiqfveXk3kbC51F52fqcajCLyxAmEB3PEXTS4Rj6fz+pozFfRs2iKgkC9f38+DJn3EvRDYeZKzq39o73yTF06JPovRZ8b7jiKpUASH1L3rMIK8E6e+7/N03gYLOosbtJ7967nvrW1WX2528/67uFTfvJnMjeDK7dLAnVB8LSz2oWxnkv3bx1dQLUCqnAoDebVmCrVxzi2gE5ea7RRHSBpYsdatNKR1ESXJcWo3REvui6AFqmKqoaAJvZhelVsrFViksYM2Xdfg5Ohb05ZndYrZh5B8aCPYB/3EI6vZgZSfmPdc3N9qIU+B+qYEfiDLX5OmiOCi8DIwcpBqGwc+j3TkBTBeF5diZy3VKM/BGB66BLmx0VI4OHTQ0ifv7vHFSmRt9WFDbEaqKfzTprKpt/nNk1Whiv3TqB7vUivdN9KsYrsQIk3jeRKezrZUB/Q4mFE4xjenm6oqULCCSlzJ+FTscvULnmMHjF9DZu6OZ8WeFdYYtIFVqiHZmRewP6f4fYy0B/8SXk90r+Q1f4VC4B212FD3x19z9sWBHDJACbC00B75E",
    "userId": "smith4035",
    "userCdDomainId": "A000000007860194",
    "primaryAccountId": "870018425",
    "lastLoginTime": "2020-05-15T12:05:25+0000",
    "tokenExpirationTime": "2020-05-15T12:35:26+0000",
    "loginTime": "2020-05-15T12:05:26+0000",
    "accessLevel": "CUS",
    "stalePassword": false,
    "streamerInfo": {
        "streamerBinaryUrl": "streamer-bin.tdameritrade.com",
        "streamerSocketUrl": "streamer-ws.tdameritrade.com",
        "token": "571280276135ac10fd62df2dfec7cfa97df27436",
        "tokenTimestamp": "2020-05-15T12:05:30+0000",
        "userGroup": "ACCT",
        "accessLevel": "ACCT",
        "acl": "AQBHBPC1DRDTESF7FUFXG1G3G5G7GKGLGRH1H3H5IPM1MAOSPNQSRFSDSVTBTETFTOTTUAWSXBXNXOD2D4D6D8E2E4E6E8F2F4F6H7I1",
        "appId": "DP"
    },
    "professionalStatus": "NON_PROFESSIONAL",
    "quotes": {
        "isNyseDelayed": false,
        "isNasdaqDelayed": false,
        "isOpraDelayed": false,
        "isAmexDelayed": false,
        "isCmeDelayed": true,
        "isIceDelayed": true,
        "isForexDelayed": true
    },
    "streamerSubscriptionKeys": {
        "keys": [
            {
                "key": "a8400203713555cb9b8dd0f0109f52841f9a6a7c1d7eded8faacb4df03ebdc518"
            }
        ]
    },
    "accounts": [
        {
            "accountId": "870018425",
            "displayName": "smith4035",
            "accountCdDomainId": "A000000008841148",
            "company": "AMER",
            "segment": "AMER",
            "acl": "AQBHBPC1DRDTESF7FUFXG1G3G5G7GKGLGRH1H3H5IPM1MAOSPNQSRFSDSVTBTETFTOTTUAWSXBXNXO",
            "authorizations": {
                "apex": false,
                "levelTwoQuotes": false,
                "stockTrading": true,
                "marginTrading": true,
                "streamingNews": false,
                "optionTradingLevel": "SPREAD",
                "streamerAccess": true,
                "advancedMargin": true,
                "scottradeAccount": false
            }
        }
    ]
}

//Converts ISO-8601 response in snapshot to ms since epoch accepted by Streamer
var tokenTimeStampAsDateObj = new Date(userPrincipalsResponse.streamerInfo.tokenTimestamp);
var tokenTimeStampAsMs = tokenTimeStampAsDateObj.getTime();

var credentials = {
    "userid": userPrincipalsResponse.accounts[0].accountId,
    "token": userPrincipalsResponse.streamerInfo.token,
    "company": userPrincipalsResponse.accounts[0].company,
    "segment": userPrincipalsResponse.accounts[0].segment,
    "cddomain": userPrincipalsResponse.accounts[0].accountCdDomainId,
    "usergroup": userPrincipalsResponse.streamerInfo.userGroup,
    "accesslevel": userPrincipalsResponse.streamerInfo.accessLevel,
    "authorized": "Y",
    "timestamp": tokenTimeStampAsMs,
    "appid": userPrincipalsResponse.streamerInfo.appId,
    "acl": userPrincipalsResponse.streamerInfo.acl
}

var testRequest = {
    "requests": [
        {
            "service": "ADMIN",
            "command": "LOGIN",
            "requestid": 0,
            "account": userPrincipalsResponse.accounts[0].accountId,
            "source": userPrincipalsResponse.streamerInfo.appId,
            "parameters": {
                "credential": jsonToQueryString(credentials),
                "token": userPrincipalsResponse.streamerInfo.token,
                "version": "1.0"
            }
        }
    ]
}


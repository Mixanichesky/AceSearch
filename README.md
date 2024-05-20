# AceSearch

1. build image:  
```
docker build -t ace_search .
```

2. Run build project: 
```
docker run -it -v <your output build path>:/src/publish --name ace_search ace_search
```

3. Wait until the container finishes its work and delete it
```
docker rm ace_search
```

4. Run AceSearch: 
```
whith default settings, file appsettings.json:
./AceSearch

custom file settings, filename.json:
./AceSearch <config file full name>
```

Sample config file:
```
{
  "availability": "0.05",
  "availabilityUpdatedAtHours": "1000",
  "createFavorite": "true",
  "createJson": "true",
  "linkToBroadcastById": "true",
  "useExternalAceSearch": "false",
  "addCategories": "true",
  "addIcons": "true",
  "externalAceSearchUrl": "https://search.acestream.net/all?api_version=1&api_key=test_api_key",
  "outputFolder": "/opt/lists/",
  "playListAllFilename": "as.m3u",
  "playListFavoriteFileName": "f.as.m3u",
  "aceStreamEnginePort": "6878",
  "favoriteChannels": "History,Оружие, Техно"
}
```

Main parametrs: 

linkToBroadcastById - true, create playlist by channel id. false, create playlist by infohash

useExternalAceSearch - true, getting playlist information from an external source (externalAceSearchUrl), false, from a local running AceStreamEngine





.NET Install: https://dotnet.microsoft.com/download/dotnet/7.0

.NET Install scripts: https://docs.microsoft.com/dotnet/core/install/linux

Sample for ubuntu 20.10 
```
wget https://packages.microsoft.com/config/ubuntu/20.10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y apt-transport-https 
sudo apt-get update 
sudo apt-get install -y dotnet-runtime-7.0
```

# AceSearch

1. build container:  
```
docker build -t ace_search .
```
2. Run build: 
```
docker run -d -v <output build path>:/src/AceSearch/src/bin/Release/net5.0 --name ace_search ace_search
```
3. Run AceSearch: 
```
dotnet AceSearch.dll <config file full name>
```

Sample config file:
```
{
  "availability": "0.05",
  "availabilityUpdatedAtHours": "1000",
  "createFavorite": "true",
  "createJson": "true",
  "outputFolder": "/opt/lists/",
  "playListAllFilename": "as.m3u",
  "playListFavoriteFileName": "f.as.m3u",
  "urlTemplate": "acestream://{0}",
  "favoriteChannels": "History,Оружие, Техно"
}
```

.NET Install: https://dotnet.microsoft.com/download/dotnet/5.0

.NET Install scripts: https://dotnet.microsoft.com/download/dotnet/scripts

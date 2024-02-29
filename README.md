# AceSearch

1. build image:  
```
docker build -t ace_search .
```
2. Run build project: 
```
docker run -d -v <output build path>:/src/AceSearch/src/bin/Release/net7.0 --name ace_search ace_search
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

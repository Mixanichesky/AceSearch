FROM mcr.microsoft.com/dotnet/sdk:5.0

WORKDIR /src
RUN git clone --recursive https://github.com/ef1f/AceSearch.git
WORKDIR /src/AceSearch
CMD dotnet build --configuration Release
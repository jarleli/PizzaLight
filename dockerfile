FROM mcr.microsoft.com/dotnet/sdk:6.0 as build
COPY ./src ./buildfolder
WORKDIR /buildfolder/
RUN dotnet test PizzaLight.sln --filter TestCategory=Unit
RUN dotnet publish -c Release -o /buildfolder/output/pizzalight PizzaLight/PizzaLight.csproj


FROM mcr.microsoft.com/dotnet/aspnet:6.0 as RUNTIME
COPY --from=BUILD /buildfolder/output ./app
WORKDIR /app/pizzalight/
ENTRYPOINT ["dotnet", "PizzaLight.dll"]

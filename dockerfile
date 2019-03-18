FROM mcr.microsoft.com/dotnet/core/sdk:2.2 as BUILD
COPY ./src ./buildfolder
WORKDIR /buildfolder/
RUN dotnet publish -c Release -o /buildfolder/output/pizzalight PizzaLight/PizzaLight.csproj

FROM mcr.microsoft.com/dotnet/core/runtime:2.2 as RUNTIME
COPY --from=BUILD /buildfolder/output ./app
WORKDIR /app/pizzalight/
ENTRYPOINT ["dotnet", "PizzaLight.dll"]

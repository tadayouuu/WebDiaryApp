# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# csproj を先にコピーして restore
COPY ./WebDiaryApp/WebDiaryApp.csproj ./WebDiaryApp.csproj
RUN dotnet restore WebDiaryApp.csproj

# ソースコード全体をコピー
COPY ./WebDiaryApp ./WebDiaryApp
WORKDIR /src/WebDiaryApp

# Publish
RUN dotnet publish WebDiaryApp.csproj -c Release -o /app/publish

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# ビルド成果物をコピー
COPY --from=build /app/publish .

# diary.db をコピー（書き込み可能に）
COPY ./WebDiaryApp/diary.db ./diary.db
RUN chmod 777 ./diary.db

# ポート設定
ENV ASPNETCORE_URLS=http://+:5000
# Development モードで詳細エラーを確認
ENV ASPNETCORE_ENVIRONMENT=Development

# 起動コマンド
ENTRYPOINT ["dotnet", "WebDiaryApp.dll"]

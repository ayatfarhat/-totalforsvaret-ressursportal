services:

  web:
    build: .
    ports:
      - "8080:8080"

    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: "Server=mariadb;Port=3306;Database=nabohjelp;User=nabohjelp_user;Password=nabohjelp_password;"

    # Bilder som brukere laster opp lagres her. Uten dette forsvinner de
    # hver gang containeren bygges på nytt.
    volumes:
      - nabohjelp_uploads:/app/wwwroot/uploads
      # Krypteringsnøklene (se Program.cs). Må beholdes, ellers kan ikke de
      # krypterte personopplysningene i databasen leses lenger.
      - nabohjelp_nokler:/app/nokler

    depends_on:
      mariadb:
        condition: service_healthy


  mariadb:
    image: mariadb:12

    environment:
      MARIADB_ROOT_PASSWORD: root_password
      MARIADB_DATABASE: nabohjelp
      MARIADB_USER: nabohjelp_user
      MARIADB_PASSWORD: nabohjelp_password

    ports:
      - "3306:3306"

    volumes:
      - nabohjelp_data:/var/lib/mysql

    healthcheck:
      test:
        [
          "CMD",
          "healthcheck.sh",
          "--connect",
          "--innodb_initialized"
        ]
      interval: 5s
      timeout: 5s
      retries: 10


volumes:
  nabohjelp_data:
  nabohjelp_uploads:
  nabohjelp_nokler:

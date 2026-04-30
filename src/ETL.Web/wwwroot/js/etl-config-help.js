(function () {
  const sourceSelect = document.getElementById("SourceType");
  const destinationSelect = document.getElementById("DestinationType");
  const sourceOutput = document.getElementById("sourceConfigExample");
  const destinationOutput = document.getElementById("destinationConfigExample");

  if (
    !sourceSelect ||
    !destinationSelect ||
    !sourceOutput ||
    !destinationOutput
  ) {
    return;
  }

  const sourceExamples = {
    1: {
      filePath: "/absolute/path/customers.csv",
      delimiter: ",",
      hasHeaderRecord: true,
    },
    2: {
      filePath: "/absolute/path/customers.xlsx",
      sheetName: "Sheet1",
      useHeaderRow: true,
    },
    3: {
      connectionString:
        "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;",
      query: "SELECT id, full_name, email FROM source_customers",
    },
    4: {
      url: "https://api.example.com/customers",
      method: "GET",
      headers: {
        Authorization: "Bearer <token>",
      },
      rootArrayProperty: "data",
      timeoutSeconds: 60,
    },
  };

  const destinationExamples = {
    1: {
      connectionString:
        "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;",
      tableName: "dbo.customers",
      keyColumns: ["id"],
      batchSize: 1000,
    },
    2: {
      connectionString:
        "Host=postgres;Port=5432;Database=etlprojectdb;Username=postgres;Password=1234",
      tableName: "public.customers",
      keyColumns: ["id"],
      batchSize: 1000,
    },
  };

  function renderExamples() {
    const sourceJson =
      sourceExamples[sourceSelect.value] ?? sourceExamples["1"];
    const destinationJson =
      destinationExamples[destinationSelect.value] ?? destinationExamples["2"];

    sourceOutput.textContent = JSON.stringify(sourceJson, null, 2);
    destinationOutput.textContent = JSON.stringify(destinationJson, null, 2);
  }

  sourceSelect.addEventListener("change", renderExamples);
  destinationSelect.addEventListener("change", renderExamples);

  renderExamples();
})();

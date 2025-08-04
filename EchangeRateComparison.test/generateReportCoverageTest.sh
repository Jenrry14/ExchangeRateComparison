#!/bin/bash

# 1) Ejecutar los tests con cobertura, dejando resultados en ./TestResults
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# 2) Encontrar el directorio más reciente dentro de ./TestResults
latestResultDir=$(ls -td ./TestResults/*/ | head -n 1)

# 3) Construir la ruta absoluta al coverage.cobertura.xml
absoluteCoverageFile="$(cd "$latestResultDir" && pwd)/coverage.cobertura.xml"

# 4) Construir el comando
cmd="reportgenerator -reports:$absoluteCoverageFile -targetdir:./reports -reporttypes:Html"

echo "------------------------------------------------"
echo "Comando que se va a ejecutar:"
echo "$cmd"
echo "------------------------------------------------"

# 5) Ejecutar el comando
eval "$cmd"

# 6) Abrir el reporte en el navegador
open ./reports/index.html

# 7) Esperar a que el usuario pulse Enter antes de continuar
echo "Presiona Enter cuando termines de ver el reporte para eliminar 'reports' y 'TestResults'..."
read

# 8) Eliminar las carpetas
rm -rf ./reports
rm -rf ./TestResults

echo "✅ Se han eliminado las carpetas 'reports' y 'TestResults'."
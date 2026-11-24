git clone https://github.com/hossein-abdn/otel.git

docker network create otel-network
docker run -d --name otel-collector --network otel-network -v "$(pwd)/otel-collector-config.yaml:/etc/otelcol/config.yaml:ro" otel/opentelemetry-collector:0.165.0 --config=/etc/otelcol/config.yaml
docker run -d --name nginx --network otel-network -p 8080:80 -v "$(pwd)/nginx.conf:/etc/nginx/nginx.conf:ro" nginx:otel
docker build -t otel-test-api .
docker run -d --name dotnet-api --network otel-network -e ASPNETCORE_URLS=http://+:8080 otel-test-api
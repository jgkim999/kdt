# OpenSearch Data Prepper를 사용한 관측성 스택

이 프로젝트는 Docker Compose를 사용하여 포괄적인 관측성 스택을 제공합니다.

OpenSearch Data Prepper를 통한 로그 처리, Prometheus를 통한 메트릭 수집, Grafana와 OpenSearch Dashboards를 통한 시각화 기능을 포함합니다.

## 아키텍처 개요

스택 구성 요소:

- **로그 처리**: OTEL Collector → Data Prepper → OpenSearch (+ 레거시 지원을 위한 Loki)
- **메트릭**: Prometheus → Grafana
- **트레이싱**: Jaeger/Tempo
- **데이터베이스**: MySQL, RabbitMQ, Valkey
- **시각화**: Grafana, OpenSearch Dashboards
- **모니터링**: cAdvisor, Node Exporter, Redis Exporter
- **관리**: Portainer

## 빠른 시작

### 사전 요구사항

[Docker 설치](https://docs.docker.com/engine/install/)

### 스택 시작하기

```bash
# 모든 서비스 시작
docker-compose up -d

# 또는 새로운 Docker Compose 문법 사용
docker compose up -d

# 특정 서비스만 시작
docker-compose up -d opensearch data-prepper otel-collector grafana
```

### 스택 중지하기

```bash
# 모든 서비스 중지
docker-compose down

# 볼륨까지 제거하여 중지 (경고: 모든 데이터가 삭제됩니다)
docker-compose down -v
```

## MySQL

[MySql](https://www.mysql.com/)

비밀번호

- root / 1234

```sql
CREATE USER 'user1'@'%' IDENTIFIED BY '1234';
GRANT ALL PRIVILEGES ON *.* TO 'user1'@'%';
FLUSH PRIVILEGES;
```

MySQL Exporter용

```sql
CREATE USER 'exporter'@'%' IDENTIFIED BY '1234qwer' WITH MAX_USER_CONNECTIONS 3;
GRANT ALL PRIVILEGES ON *.* TO 'exporter'@'%';
FLUSH PRIVILEGES;
```

## RabbitMQ

[RabbitMQ](https://www.rabbitmq.com/)

사용자/비밀번호

- user/1234

## OpenSearch Data Prepper 파이프라인

### 개요

OpenSearch Data Prepper는 OTEL Collector로부터 로그를 처리하고, 변환하여 고급 검색 및 분석 기능을 위해 OpenSearch에 인덱싱합니다.

### 파이프라인 흐름

```
애플리케이션 → OTEL Collector → Data Prepper → OpenSearch → OpenSearch Dashboards
                     ↓
                   Loki → Grafana (레거시)
```

### 설정 파일

- `data-prepper.yaml` - 메인 파이프라인 설정
- `opensearch-logs-template.json` - 로그 매핑을 위한 인덱스 템플릿
- `.env.data-prepper` - 환경 변수

### 서비스 엔드포인트

- **Data Prepper OTLP**: `localhost:21892` (gRPC)
- **Data Prepper API**: `localhost:4900` (HTTP)
- **Data Prepper 메트릭**: `localhost:9600` (Prometheus)
- **OpenSearch**: `localhost:9200`
- **OpenSearch Dashboards**: `localhost:5601`

### Data Prepper로 로그 전송

#### OTEL Collector 사용 (자동)

스택이 실행 중일 때 로그가 OTEL Collector에서 Data Prepper로 자동으로 라우팅됩니다.

#### 직접 OTLP 제출

```bash
# 예시: 테스트 로그 직접 전송
curl -X POST http://localhost:4318/v1/logs \
  -H "Content-Type: application/json" \
  -d '{
    "resourceLogs": [{
      "resource": {
        "attributes": [{
          "key": "service.name",
          "value": {"stringValue": "test-service"}
        }]
      },
      "scopeLogs": [{
        "logRecords": [{
          "timeUnixNano": "'$(date +%s%N)'",
          "severityText": "INFO",
          "body": {"stringValue": "테스트 로그 메시지"}
        }]
      }]
    }]
```

### 로그 쿼리 예시

#### OpenSearch REST API

```bash
# 모든 로그 검색
curl -X GET "localhost:9200/logs-*/_search?pretty"

# 서비스 이름으로 검색
curl -X GET "localhost:9200/logs-*/_search?pretty" \
  -H "Content-Type: application/json" \
  -d '{
    "query": {
      "match": {
        "service_name": "my-service"
      }
    }
  }'

# 로그 레벨로 검색
curl -X GET "localhost:9200/logs-*/_search?pretty" \
  -H "Content-Type: application/json" \
  -d '{
    "query": {
      "match": {
        "severity_text": "ERROR"
      }
    }
  }'

# 시간 범위 쿼리 (최근 1시간)
curl -X GET "localhost:9200/logs-*/_search?pretty" \
  -H "Content-Type: application/json" \
  -d '{
    "query": {
      "range": {
        "@timestamp": {
          "gte": "now-1h"
        }
      }
    }
  }'

# 필터가 포함된 복합 쿼리
curl -X GET "localhost:9200/logs-*/_search?pretty" \
  -H "Content-Type: application/json" \
  -d '{
    "query": {
      "bool": {
        "must": [
          {"match": {"service_name": "web-app"}},
          {"range": {"@timestamp": {"gte": "now-24h"}}}
        ],
        "filter": [
          {"term": {"severity_text": "ERROR"}}
        ]
      }
    },
    "sort": [{"@timestamp": {"order": "desc"}}],
    "size": 100
  }'
```

#### OpenSearch Dashboards 쿼리

`http://localhost:5601`에서 OpenSearch Dashboards에 접속하여 Discover에서 다음 쿼리 예시를 사용하세요:

```
# 기본 텍스트 검색
service_name:"web-app" AND severity_text:"ERROR"

# 와일드카드를 사용한 시간 범위
@timestamp:[now-1h TO now] AND message:*exception*

# 필드 존재 여부
_exists_:trace_id AND service_name:"api-service"

# 범위 쿼리
response_time:[100 TO 500] AND status_code:>=400
```

### Dashboard Usage

#### OpenSearch Dashboards

1. **Access**: Navigate to `http://localhost:5601`
2. **Index Pattern**: Create index pattern `logs-*` to view all log indices
3. **Discover**: Use the Discover tab to search and filter logs
4. **Visualizations**: Create charts and graphs from log data
5. **Dashboards**: Combine visualizations into comprehensive dashboards

#### Grafana Dashboards

1. **Access**: Navigate to `http://localhost:3000` (admin/admin)
2. **Data Prepper Metrics**: Import the dashboard from `grafana-data-prepper-dashboard.json`
3. **Key Metrics**:
   - Log processing rate
   - Processing latency
   - Error rates
   - Memory and CPU usage

### Monitoring and Alerting

#### Data Prepper Health Check

```bash
# Check Data Prepper health
curl -X GET http://localhost:4900/health

# Expected response
{
  "status": "GREEN",
  "statusReason": "All components are healthy"
}
```

#### Prometheus Metrics

```bash
# View all Data Prepper metrics
curl -X GET http://localhost:9600/metrics

# Key metrics to monitor:
# - dataprepper_logs_received_total
# - dataprepper_logs_processed_total
# - dataprepper_processing_errors_total
# - dataprepper_processing_time_seconds
```

#### Log Processing Verification

```bash
# Check if logs are being processed
./verify_opensearch_logs.py

# Generate test logs for verification
python3 generate_test_logs.py
```

## 서비스 설정

### OpenSearch

- **URL**: `http://localhost:9200`
- **대시보드**: `http://localhost:5601`
- **기본 인덱스 패턴**: `logs-YYYY.MM.dd`

### Data Prepper

- **설정**: `data-prepper.yaml`
- **환경**: `.env.data-prepper`
- **데이터 디렉토리**: `./data-prepper-data`
- **DLQ 디렉토리**: `./data-prepper-data/dlq`

### 추가 서비스

#### Portainer
- **URL**: `http://localhost:9000`
- **설명**: Docker 컨테이너 관리 웹 UI

#### Prometheus
- **URL**: `http://localhost:9090`
- **설명**: 메트릭 수집 및 모니터링

#### Jaeger
- **URL**: `http://localhost:16686`
- **설명**: 분산 트레이싱 UI

#### Tempo
- **URL**: `http://localhost:3200`
- **설명**: 분산 트레이싱 백엔드

#### Valkey
- **포트**: `6379`
- **설명**: Redis 호환 인메모리 데이터베이스

## Grafana

[Grafana](https://grafana.com/)

사용자/비밀번호

- admin/admin

## Troubleshooting Guide

### Common Issues and Solutions

#### Data Prepper Not Starting

**Symptoms**: Data Prepper container exits or fails to start

**Solutions**:

```bash
# Check container logs
docker-compose logs data-prepper

# Verify configuration syntax
docker run --rm -v $(pwd)/data-prepper.yaml:/usr/share/data-prepper/pipelines/pipelines.yaml \
  opensearchproject/data-prepper:2.10.0 \
  /usr/share/data-prepper/bin/data-prepper --validate-pipeline-configuration

# Check file permissions
ls -la data-prepper.yaml
chmod 644 data-prepper.yaml

# Restart with fresh configuration
docker-compose restart data-prepper
```

#### No Logs Appearing in OpenSearch

**Symptoms**: Logs sent to OTEL Collector but not visible in OpenSearch

**Diagnosis Steps**:

```bash
# 1. Check OTEL Collector logs
docker-compose logs otel-collector

# 2. Check Data Prepper logs
docker-compose logs data-prepper

# 3. Verify Data Prepper is receiving logs
curl -X GET http://localhost:9600/metrics | grep dataprepper_logs_received_total

# 4. Check OpenSearch indices
curl -X GET "localhost:9200/_cat/indices/logs-*?v"

# 5. Test direct log submission
curl -X POST http://localhost:21892/log/ingest \
  -H "Content-Type: application/json" \
  -d '{"message": "test log", "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%S.%3NZ)'"}'
```

**Common Fixes**:

- Verify network connectivity between services
- Check Data Prepper pipeline configuration
- Ensure OpenSearch is healthy and accepting connections
- Verify index template is properly applied

#### High Memory Usage

**Symptoms**: Data Prepper consuming excessive memory

**Solutions**:

```bash
# Check current memory usage
docker stats data-prepper

# Adjust JVM heap size in .env.data-prepper
echo "DATA_PREPPER_JAVA_OPTS=-Xms512m -Xmx1g" >> .env.data-prepper

# Tune buffer sizes in data-prepper.yaml
# Reduce buffer_size and batch_size in processors

# Restart with new settings
docker-compose restart data-prepper
```

#### Processing Delays

**Symptoms**: Logs appearing in OpenSearch with significant delay

**Diagnosis**:

```bash
# Check processing metrics
curl -X GET http://localhost:9600/metrics | grep processing_time

# Monitor queue sizes
curl -X GET http://localhost:9600/metrics | grep queue_size

# Check OpenSearch bulk indexing performance
curl -X GET "localhost:9200/_nodes/stats/indices/indexing"
```

**Optimizations**:

- Increase batch_size in OpenSearch sink
- Adjust flush_timeout settings
- Scale Data Prepper horizontally if needed
- Optimize OpenSearch cluster settings

#### Dead Letter Queue Issues

**Symptoms**: Logs appearing in DLQ instead of OpenSearch

**Investigation**:

```bash
# Check DLQ directory
ls -la ./data-prepper-data/dlq/

# Examine failed logs
cat ./data-prepper-data/dlq/failed-logs-*.json

# Check Data Prepper error logs
docker-compose logs data-prepper | grep ERROR
```

**Resolution**:

- Fix data format issues in source logs
- Adjust grok patterns for log parsing
- Verify OpenSearch mapping compatibility
- Increase retry limits if transient failures

#### OpenSearch Connection Issues

**Symptoms**: Data Prepper cannot connect to OpenSearch

**Checks**:

```bash
# Test OpenSearch connectivity from Data Prepper container
docker-compose exec data-prepper curl -X GET http://opensearch:9200/_cluster/health

# Check OpenSearch logs
docker-compose logs opensearch

# Verify network configuration
docker network ls
docker network inspect $(docker-compose ps -q opensearch | head -1)
```

#### Performance Monitoring

**Key Metrics to Monitor**:

```bash
# Log processing rate (logs/second)
curl -s http://localhost:9600/metrics | grep "dataprepper_logs_processed_total"

# Processing latency (milliseconds)
curl -s http://localhost:9600/metrics | grep "dataprepper_processing_time_seconds"

# Error rate
curl -s http://localhost:9600/metrics | grep "dataprepper_processing_errors_total"

# Memory usage
curl -s http://localhost:9600/metrics | grep "jvm_memory_used_bytes"

# OpenSearch indexing rate
curl -X GET "localhost:9200/_nodes/stats/indices/indexing" | jq '.nodes[].indices.indexing.index_total'
```

### Service Management Commands

#### Start/Stop Individual Services

```bash
# Start only core logging services
docker-compose up -d opensearch data-prepper otel-collector

# Stop Data Prepper for maintenance
docker-compose stop data-prepper

# Restart with configuration changes
docker-compose restart data-prepper

# View service status
docker-compose ps
```

#### Configuration Reload

```bash
# Reload Data Prepper configuration (requires restart)
docker-compose restart data-prepper

# Reload OTEL Collector configuration
docker-compose restart otel-collector

# Apply new OpenSearch template
curl -X PUT "localhost:9200/_index_template/logs-template" \
  -H "Content-Type: application/json" \
  -d @opensearch-logs-template.json
```

#### Data Management

```bash
# Clean old log indices (older than 30 days)
curl -X DELETE "localhost:9200/logs-$(date -d '30 days ago' +%Y.%m.%d)"

# Check index sizes
curl -X GET "localhost:9200/_cat/indices/logs-*?v&s=store.size:desc"

# Force index refresh
curl -X POST "localhost:9200/logs-*/_refresh"

# Clear DLQ files
rm -f ./data-prepper-data/dlq/*
```

### Emergency Procedures

#### Complete Stack Reset

```bash
# Stop all services
docker-compose down

# Remove all data (WARNING: This deletes all logs and metrics)
docker-compose down -v
rm -rf ./data-prepper-data/*

# Start fresh
docker-compose up -d
```

#### Data Prepper Recovery

```bash
# Backup current configuration
cp data-prepper.yaml data-prepper.yaml.backup

# Reset to minimal configuration
cat > data-prepper-minimal.yaml << EOF
log-pipeline:
  source:
    otel_logs_source:
      port: 21892
  sink:
    - stdout:
EOF

# Test with minimal config
docker run --rm -p 21892:21892 \
  -v $(pwd)/data-prepper-minimal.yaml:/usr/share/data-prepper/pipelines/pipelines.yaml \
  opensearchproject/data-prepper:2.10.0

# Gradually add back features once basic functionality works
```

### Getting Help

- **Data Prepper Documentation**: <https://opensearch.org/docs/latest/data-prepper/>
- **OpenSearch Documentation**: <https://opensearch.org/docs/latest/>
- **OTEL Collector Documentation**: <https://opentelemetry.io/docs/collector/>
- **Issue Tracking**: Check container logs and metrics endpoints for detailed error information

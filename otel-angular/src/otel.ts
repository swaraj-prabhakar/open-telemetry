import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { WebTracerProvider, BatchSpanProcessor } from '@opentelemetry/sdk-trace-web';
import { getWebAutoInstrumentations } from '@opentelemetry/auto-instrumentations-web';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { ZoneContextManager } from '@opentelemetry/context-zone-peer-dep';
import { Resource } from '@opentelemetry/resources';
import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions';
 
const provider = new WebTracerProvider({
    resource: new Resource({
        [SemanticResourceAttributes.SERVICE_NAME]: 'spa-client'
    })
});
 
// For demo purposes only, immediately log traces to the console
// provider.addSpanProcessor(new BatchSpanProcessor(new ConsoleSpanExporter()));
 
provider.addSpanProcessor(
    new BatchSpanProcessor(
        new OTLPTraceExporter({
            url: 'http://localhost:4318/v1/traces'
        }),
    ),
);
 
provider.register({
  contextManager: new ZoneContextManager(),
});
 
 
registerInstrumentations({
    instrumentations: [
        getWebAutoInstrumentations({
            '@opentelemetry/instrumentation-xml-http-request': {
                propagateTraceHeaderCorsUrls: /.*/
            },
            '@opentelemetry/instrumentation-document-load': {},
            '@opentelemetry/instrumentation-fetch': {},
            '@opentelemetry/instrumentation-user-interaction': {}
          }),
    ],
});
# Pruebas del servicio de email

## Revision realizada

Se ha ejecutado la suite C# completa con cobertura: **449 pruebas correctas,
0 fallidas y 0 omitidas**, incluidas las de integracion con Testcontainers.
Estos tests usan mocks SMTP o memoria; no envian correos reales a Gmail.

El informe `coverage.cobertura.xml` generado en `TestResults/EmailAuditFinal` mide el codigo
ejecutado por los tests C#, no la cobertura de las peticiones de Bruno.

| Codigo | Lineas cubiertas |
| --- | --- |
| MailKitEmailService | 100 % |
| MemoryEmailService | 100 % |
| EmailBackgroundWorker | 100 % |
| EmailQueueService | 100 % |
| EmailMessage y SmtpSettings | 100 % |

El 100 % de lineas **no significa** el 100 % de ramas. Queda sin ejecutar
la alternativa defensiva `null` de `TryDequeue` en la cola: cada `Release`
corresponde a un mensaje, de modo que el recorrido normal no la necesita.
No se han usado reflection ni exclusiones de cobertura para forzar ese caso.
La cobertura global de la API en esta ejecucion es **80,04 % de lineas y
61,05 % de ramas**, no el 100 % del proyecto.

## Comprobaciones reforzadas

- Borrado logico: un correo con destinatario, asunto, cuerpo y tipo correctos
  solo cuando se elimina el Funko; ninguno si no existe o no se borra.
- Borrado fisico: se comprueba el correo tras confirmar la transaccion real
  de PostgreSQL; ninguno cuando falla y se realiza rollback.
- Worker: crea y libera un scope distinto para cada mensaje, tambien si
  falla el primer envio y continua con el segundo.
- SMTP: propaga la cancelacion y no autentica ni envia tras fallar la conexion.
- Memoria: el listado devuelto es una copia y `Clear` funciona con la coleccion vacia.

## Que comprueba Bruno

La carpeta `4 - FUNKOS` ya recorre creaciones con y sin imagen, errores,
permisos y borrados logicos. Se ha anadido `[050] DELETE - Repetir borrado
devuelve 404`: reutiliza el Funko eliminado en `[044]` y comprueba que no se
acepta de nuevo la operacion.

**La peticion nueva esta revisada estaticamente, pero no se ha ejecutado.**
El contenedor estaba configurado con `Email__UseSmtp=true` al revisarlo y
no se ha lanzado la coleccion para evitar envios reales.

Bruno no verifica el buzon ni el contenido de `MemoryEmailService` porque
no hay un endpoint que lo exponga. No se ha creado un endpoint de depuracion
solo para satisfacer una prueba. `HardDelete` tampoco tiene una ruta HTTP:
se prueba en C#, no se inventa una ruta en Bruno.

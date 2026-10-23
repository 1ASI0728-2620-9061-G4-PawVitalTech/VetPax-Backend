
# language: es
@appointment
Característica: US04 US05 US06 - Gestión de citas veterinarias
  Como usuario de VetPax
  Quiero gestionar citas con permisos y disponibilidad
  Para organizar la atención veterinaria de las mascotas

  Antecedentes:
    Dado que existe una clínica preparada para gestionar citas

  Escenario: US04 - Propietario agenda una cita disponible
    Cuando el propietario agenda una cita a las 09:00
    Entonces el código HTTP de citas es 201
    Y la respuesta de cita tiene estado Pending

  Escenario: US04 - No se permite reservar dos veces el mismo horario
    Dado que existe una cita pendiente a las 09:00
    Cuando el propietario agenda una cita a las 09:00
    Entonces el código HTTP de citas es 409

  Escenario: US04 - Otro propietario no puede agendar para una mascota ajena
    Cuando otro propietario intenta agendar una cita
    Entonces el código HTTP de citas es 403

  Escenario: US04 - Un veterinario no puede agendar como propietario
    Cuando un veterinario intenta agendar una cita
    Entonces el código HTTP de citas es 403

  Escenario: US05 - Propietario reprograma una cita pendiente
    Dado que existe una cita pendiente a las 09:00
    Cuando el propietario reprograma la cita a las 10:00
    Entonces el código HTTP de citas es 200
    Y la respuesta de cita inicia a las 10:00

  Escenario: US05 - Cancelar una cita libera el horario
    Dado que existe una cita pendiente a las 09:00
    Cuando el propietario cancela la cita
    Entonces el código HTTP de citas es 200
    Y la respuesta de cita tiene estado Cancelled
    Cuando el propietario agenda una cita a las 09:00
    Entonces el código HTTP de citas es 201

  Escenario: US05 - No se puede reprogramar una cita cancelada
    Dado que existe una cita pendiente a las 09:00
    Y el propietario cancela la cita
    Cuando el propietario reprograma la cita a las 10:00
    Entonces el código HTTP de citas es 409

  Escenario: TS03 - Un propietario ajeno no consulta una cita
    Dado que existe una cita pendiente a las 09:00
    Cuando otro propietario consulta la cita
    Entonces el código HTTP de citas es 403

  Escenario: US06 - Veterinario consulta su agenda ordenada
    Dado que existe una cita pendiente a las 10:00
    Y existe otra cita pendiente a las 09:00
    Cuando el veterinario consulta su agenda
    Entonces el código HTTP de citas es 200
    Y la agenda contiene dos citas ordenadas

  Escenario: US06 - Un veterinario no vinculado no consulta la agenda
    Cuando un veterinario ajeno consulta la agenda
    Entonces el código HTTP de citas es 403

  Escenario: US06 - Propietario no puede marcar una cita como atendida
    Dado que existe una cita pendiente a las 09:00
    Cuando el propietario intenta marcar la cita como atendida
    Entonces el código HTTP de citas es 403

  Escenario: US06 - No se admite una atención clínica sin asociación verificada
    Dado que existe una cita pendiente a las 09:00
    Cuando el veterinario intenta marcar la cita sin atención vinculada
    Entonces el código HTTP de citas es 403

  Escenario: US06 - Veterinario confirma una cita con atención verificada
    Dado que existe una cita pendiente a las 09:00
    Y existe una atención clínica vinculada como dato de prueba
    Cuando el veterinario marca la cita como atendida
    Entonces el código HTTP de citas es 200
    Y la respuesta de cita tiene estado Attended

# language: es

Característica: US01 - Registrar mascota

Escenario: Registro exitoso
    Dado que soy un propietario nuevo
    Cuando registro una mascota válida
    Entonces la respuesta HTTP es 201
    Y se crea un historial clínico vacío

Escenario: Registro sin nombre
    Dado que soy un propietario nuevo
    Cuando intento registrar una mascota sin nombre
    Entonces la respuesta HTTP es 400
# language: es

Característica: US02 - Consultar historial clínico

Escenario: Consultar historial vacío
    Dado que ya registré una mascota
    Cuando consulto su historial como propietario
    Entonces la respuesta HTTP es 200
    Y el historial contiene cero atenciones

Escenario: Acceso no autorizado
    Dado que ya registré una mascota
    Cuando otro propietario consulta su historial
    Entonces la respuesta HTTP es 403
# language: es
@clinic
Característica: US10 y US12 - Clínica y pacientes autorizados

Escenario: Administrador crea una clínica
    Dado que soy administrador de clínica
    Cuando creo una clínica con horarios válidos
    Entonces el código HTTP de clínica es 201

Escenario: Administrador actualiza horarios válidos
    Dado que administro una clínica
    Cuando actualizo el perfil con horarios válidos
    Entonces el código HTTP de clínica es 200

Escenario: Rechazo de horarios superpuestos
    Dado que administro una clínica
    Cuando actualizo el perfil con horarios superpuestos
    Entonces el código HTTP de clínica es 400

Escenario: Veterinario no puede actualizar el perfil
    Dado que administro una clínica
    Cuando un veterinario modifica el perfil
    Entonces el código HTTP de clínica es 403

Escenario: Administrador no vincula una cuenta sin rol veterinario
    Dado que administro una clínica
    Cuando intento vincular una cuenta no veterinaria
    Entonces el código HTTP de clínica es 403

Escenario: Veterinario consulta un paciente realmente vinculado
    Dado que administro una clínica
    Y tengo un veterinario vinculado
    Y hay una mascota vinculada a la clínica
    Cuando el veterinario consulta los pacientes
    Entonces el código HTTP de clínica es 200
    Y la lista contiene la mascota vinculada

Escenario: Veterinario no vinculado no consulta pacientes
    Dado que administro una clínica
    Y tengo un veterinario vinculado
    Cuando otro veterinario consulta los pacientes
    Entonces el código HTTP de clínica es 403

Escenario: Clínica sin pacientes devuelve lista vacía
    Dado que administro una clínica
    Y tengo un veterinario vinculado
    Cuando el veterinario consulta los pacientes
    Entonces el código HTTP de clínica es 200
    Y la lista de pacientes está vacía
# language: es
@clinic
Característica: US03 y US11 - Acceso clínico veterinario autorizado

Escenario: Veterinario autorizado registra una atención
    Dado que administro una clínica
    Y tengo un veterinario vinculado
    Y hay una mascota vinculada a la clínica
    Cuando el veterinario registra una atención clínica válida
    Entonces el código HTTP clínico es 201

Escenario: Propietario no puede registrar una atención clínica
    Dado que administro una clínica
    Y tengo un veterinario vinculado
    Y hay una mascota vinculada a la clínica
    Cuando el propietario intenta registrar una atención clínica
    Entonces el código HTTP clínico es 403

Escenario: Veterinario autorizado consulta evolución con datos insuficientes
    Dado que administro una clínica
    Y tengo un veterinario vinculado
    Y hay una mascota vinculada a la clínica
    Cuando el veterinario consulta la evolución clínica
    Entonces el código HTTP clínico es 200

Escenario: Veterinario de otra clínica no consulta la evolución
    Dado que administro una clínica
    Y tengo un veterinario vinculado
    Y hay una mascota vinculada a la clínica
    Cuando otro veterinario consulta la evolución clínica
    Entonces el código HTTP clínico es 403

Escenario: Veterinario no registra atención de mascota desvinculada
    Dado que administro una clínica
    Y tengo un veterinario vinculado
    Y hay una mascota vinculada a la clínica
    Y la mascota fue desvinculada de la clínica
    Cuando el veterinario registra una atención clínica válida
    Entonces el código HTTP clínico es 403
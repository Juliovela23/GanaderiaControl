namespace GanaderiaControl.Models;

public enum EstadoReproductivo { Abierta = 0, Gestante = 1, Lactando = 2, Secada = 3 }
public enum TipoServicio { Monta = 0, InseminacionArtificial = 1 }
public enum ResultadoGestacion { NoDeterminado = 0, Gestante = 1, NoGestante = 2 }
public enum TipoParto { Normal = 0, Distocia = 1 }
public enum SexoCria { Hembra = 0, Macho = 1 }
public enum TipoAlerta { ChequeoGestacion = 0, Secado = 1, PartoProbable = 2, PartoVencido = 3, Salud = 4 }
public enum EstadoAlerta { Pendiente = 0, Notificada = 1, Atendida = 2, Vencida = 3 }

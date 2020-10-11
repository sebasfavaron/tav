using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class SimulationTest : MonoBehaviour
{

    /*
        el reconnect no anda
        
        falta implementar client prediction y server reconciliation
            + hacer que el cliente aplique los movimientos tambien (prediction) y que ahora la posicion del servidor no afecte directamente al cliente
            - tener un mundo paralelo en el cliente donde cuando recibe la posicion del servidor y el step (hasta donde ejecuto el server) aplique desde esa posicion los proximos steps,
                y chequee si termino en la misma posicion que el cubo en el mundo real en ese step. Si no es la misma posicion, tpear el cubo del mundo real a la posicion equivalente 
                del cubo en el mundo paralelo (reconciliation)
            - para que el paso anterior funcione, usar FixedUpdate
        
        TODO: ojo en el futuro con sacar un cubo de la lista ya que los puertos de todos se correrian...
    */
    
}
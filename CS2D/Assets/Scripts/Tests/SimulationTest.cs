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
            + tener un mundo paralelo en el cliente donde cuando recibe la posicion del servidor y el step (hasta donde ejecuto el server) aplique desde esa posicion los proximos steps,
                y tpear el cubo del mundo real a la posicion equivalente del cubo en el mundo paralelo (reconciliation)
            + para que el paso anterior funcione, usar Update para input y FixedUpdate para lo demas
        
        + pasar applies a fixedupdate
        + en fixed de client aplicar input y enviar paquete con input (con un input number)
    
        TODO: OJO QUE POR AHI CONCILIATE USA UNA LAYER DISTINTA, CLONAR EL TP DE JUAN Y VER
        
        ver de agregar un mensaje inicial con todos los jugadores que ya se conectaron (cuando ande reconciliacion con un solo cliente ja..)
        tambien, si el random que manda el client ya lo mando otro client no se va a poder conectar. Habria que generar otro hasta que conecte o hacer algo
    */
    
}
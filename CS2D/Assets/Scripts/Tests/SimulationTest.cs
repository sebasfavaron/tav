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
    
        + ver de agregar un mensaje inicial con todos los jugadores que ya se conectaron
        tambien, si el random que manda el client ya lo mando otro client no se va a poder conectar. Habria que generar otro hasta que conecte o hacer algo
        
        TODO: el bot queda en el aire, parece que nadie lo actualiza
        
        todo:
            Mostrar puntos y vida en client con GUI,
            que el follow de la camara sea mas smooth,
            
            sonido de bala y muerte,
            efecto de disparo, 
            efecto de daño (ese coloreo rojo con lerp y timer seria ideal), 
            key para salto y plataformas, 
            mejor modelo de tanque

            en cliente modificar ip con input text, todo: hecho pero guarda porque para hacerlo bien deberias saber el ip de cada client (pensa que el Utils.Send lo usan tanto client como server)
    */
    
}
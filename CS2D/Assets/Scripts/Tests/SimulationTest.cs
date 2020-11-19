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
        
        TODO: puedo mover al client, pero cuando miro la scene el client es un player y el primer player que se une al server es mi objeto client-{clientId} y reconciliate-{clientId}.
        todo Capaz reconciliate esta unido al equivocado
        
        TODO te swappea con el primer player(le pone el nombre client-{clientId})! por eso la camara lo sigue a el y no a vos. Vos controlas al otro (originalmente vos) porque tenes una referencia al cubeEntity
        todo soy el 2do y me considera un player (no client) porque me interpola y no me sigue la camara, pero si me toma los inputs
        
        todo sin reconciliate, solo interpolacion: interpola el ultimo player con los movimientos del client (pos y rot). El client solo se mueve (pos).
        todo que deberia pasar? que el ultimo player no se mueva (porque no es su input) y el client tampoco (porque no debe interpolar)
        todo que sea el ultimo player y no el 1ro debe tener que ver con que no habia ningun player conectado esta vez (las anteriores estaba el 1ro y otros conectados)
        
        todo mañana hacer step into a ver si spawnea en 0,3,0 y al toque se tpea al 1er player
    */
    
}
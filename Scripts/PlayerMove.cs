using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    //불러오기
    public GameManager gameManager;
    public AudioClip audioJump;
    public AudioClip audioAttack;
    public AudioClip audioDamaged;
    public AudioClip audioItem;
    public AudioClip audioDie;
    public AudioClip audioFinish;
    public float maxSpeed;
    public float jumpPower;

    Rigidbody2D rigid;
    SpriteRenderer spriteRenderer;
    Animator anim;
    CapsuleCollider2D capsulecollider;
    AudioSource audioSource;



    void Awake()
    {
        //초기화
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        capsulecollider = GetComponent<CapsuleCollider2D>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        //Jump
        if (Input.GetButtonDown("Jump") && !anim.GetBool("isJumping")) { 
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            anim.SetBool("isJumping", true);
            PlaySound("JUMP");
            audioSource.Play();
        }


        //Stop Speed
        if (Input.GetButtonUp("Horizontal"))
        {
            //normalized: 벡터 크기를 1로 만든 상태
            rigid.velocity = new Vector2(rigid.velocity.normalized.x * 0.5f, rigid.velocity.y);
        }
       

        //방향 전환 Direction Sprite
        if(Input.GetButton("Horizontal"))
            spriteRenderer.flipX = Input.GetAxisRaw("Horizontal") == -1;

        //Animation- Walk
        if (Mathf.Abs(rigid.velocity.x) < 0.3)
        {
            anim.SetBool("isWalking", false);
        }
        else
        {
            anim.SetBool("isWalking", true);
        }


    }

    void FixedUpdate()
    {
        //Move Speed
        float h = Input.GetAxisRaw("Horizontal");
        rigid.AddForce(Vector2.right * h, ForceMode2D.Impulse);

        if (rigid.velocity.x > maxSpeed)  //Right Max Speed
        { 
            rigid.velocity = new Vector2(maxSpeed, rigid.velocity.y);
        }
        else if (rigid.velocity.x < maxSpeed * (-1)) //Left Max Speed
        { 
            rigid.velocity = new Vector2(maxSpeed * (-1), rigid.velocity.y);
        }

        //Landing Platform 
        //RayCast: 오브젝트 검색을 위해 Ray를 쏘는 방식
        //DrawRay(): 에디터 상에서만 Ray를 그려주는 함수
       

        
        if (rigid.velocity.y < 0)
        {
            Debug.DrawRay(rigid.position, Vector3.down, new Color(0, 1, 0));
            //RayCastHit: Ray에 닿은 오브젝트, GetMask(): 레이어 이름에 해당하는 정수값을 리턴하는 함수
            RaycastHit2D rayHit = Physics2D.Raycast(rigid.position, Vector3.down, 1, LayerMask.GetMask("Platform"));
            if(rayHit.collider != null)
            {                
                //distance: Ray에 닿았을 때의 거리
                if(rayHit.distance < 0.5f)
                {
                   //Debug.Log(rayHit.collider.name); 
                   anim.SetBool("isJumping", false);
                }
            }
        }
        
       
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy") {
            //낙하중이면서 적을 밟으면, ATTACK
            if (rigid.velocity.y < 0 && transform.position.y > collision.transform.position.y)
            {
                OnAttack(collision.transform);
            }
            else //Damaged
                OnDamaged(collision.transform.position);
        }

        if (collision.gameObject.tag == "Spike")
        {
                OnDamaged(collision.transform.position);
            
        }
        
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {   


        if(collision.gameObject.tag == "Item")
        {
            //Point
            bool isWhite = collision.gameObject.name.Contains("White"); //Contains(비교문): 대상 문자열에 비교문이 있으면 true
            bool isGold= collision.gameObject.name.Contains("Gold");

            if (isWhite)
            {
                gameManager.stagePoint += 50;
            }
            else if (isGold)
            {
                gameManager.stagePoint += 200;
            }

            // Deactive Item
            collision.gameObject.SetActive(false);

            //Sound
            PlaySound("ITEM");
            audioSource.Play();
        }
        else if (collision.gameObject.tag == "Finish") 
        {   //Next Stage
            gameManager.NextStage();
            //Sound
            PlaySound("FINISH");
            audioSource.Play();
        }
    }
    void OnAttack(Transform enemy)
    {
        //Point
        gameManager.stagePoint += 100;

        //Reaction Force
        rigid.AddForce(Vector2.up * 10, ForceMode2D.Impulse);
        
        //Enemy Die
        EnemyMove enemyMove = enemy.GetComponent<EnemyMove>();
        enemyMove.OnDamaged();

        //Sound
        PlaySound("ATTACK");
        audioSource.Play();
    }

    //무적효과 함수
    void OnDamaged(Vector2 targetPos)
    {
        //Health Down
        gameManager.HealthDown();

        //Change Layer(Immortal Active)
        gameObject.layer = 11;

        //Sound
        PlaySound("DAMAGED");
        audioSource.Play();

        //View Alpha
        spriteRenderer.color = new Color(1, 1, 1, 0.4f);

        //Reaction Force
        int dirc = transform.position.x - targetPos.x > 0 ? 1 : -1;
        rigid.AddForce(new Vector2(dirc,1)*7, ForceMode2D.Impulse);

        //Animation
        anim.SetTrigger("doDamaged");

        //무적시간 해제
        Invoke("OffDamaged", 2);   
    }

    //무적해제
    void OffDamaged()
    {
    //Change Layer(Immortal Active)
    gameObject.layer = 10;

    //View Alpha
    spriteRenderer.color = new Color(1, 1, 1, 1);
    }

    public void OnDie()
    {
        //Sprite Alpha 투명화
        spriteRenderer.color = new Color(1, 1, 1, 0.4f);
        //Sprite FlipY 뒤집기
        spriteRenderer.flipY = true;
        //Sound
        PlaySound("DIE");
        audioSource.Play();
        //Collider disable 콜라이더 비활성화
        capsulecollider.enabled = false;
        //Die Effect Jump 죽는모션
        rigid.AddForce(Vector2.up * 5, ForceMode2D.Impulse);

    }

    public void VelocityZero()
    {
        rigid.velocity = Vector2.zero;
    }

    public void PlaySound(string action)
    {
        switch (action)
        {
            case "JUMP":
                audioSource.clip = audioJump;
                break;
            case "ATTACK":
                audioSource.clip = audioAttack;
                break;
            case "DAMAGED":
                audioSource.clip = audioDamaged;
                break;
            case "ITEM":
                audioSource.clip = audioItem;
                break;
            case "DIE":
                audioSource.clip = audioDie;
                break;
            case "FINISH":
                audioSource.clip = audioFinish;
                break;
        }
    }
}

Imports System
Imports System.IO
Imports System.Text
Imports System.Security.Cryptography

' <summary>
' This class uses a symmetric key algorithm (Rijndael/AES) to encrypt and 
' decrypt data. As long as encryption and decryption routines use the same 
' parameters to generate the keys, the keys are guaranteed to be the same.
' The class uses static functions with duplicate code to make it easier to 
' demonstrate encryption and decryption logic. In a real-life application, 
' this may not be the most efficient way of handling encryption, so - as 
' soon as you feel comfortable with it - you may want to redesign this class.
' </summary>
Public Class RijndaelSimple

    ' <summary>
    ' Encrypts specified plaintext using Rijndael symmetric key algorithm
    ' and returns a base64-encoded result.
    ' </summary>
    ' <param name="plainText">
    ' Plaintext value to be encrypted.
    ' </param>
    ' <param name="passPhrase">
    ' Passphrase from which a pseudo-random password will be derived. The 
    ' derived password will be used to generate the encryption key. 
    ' Passphrase can be any string. In this example we assume that this 
    ' passphrase is an ASCII string.
    ' </param>
    ' <param name="saltValue">
    ' Salt value used along with passphrase to generate password. Salt can 
    ' be any string. In this example we assume that salt is an ASCII string.
    ' </param>
    ' <param name="hashAlgorithm">
    ' Hash algorithm used to generate password. Allowed values are: "MD5" and
    ' "SHA1". SHA1 hashes are a bit slower, but more secure than MD5 hashes.
    ' </param>
    ' <param name="passwordIterations">
    ' Number of iterations used to generate password. One or two iterations
    ' should be enough.
    ' </param>
    ' <param name="initVector">
    ' Initialization vector (or IV). This value is required to encrypt the 
    ' first block of plaintext data. For RijndaelManaged class IV must be 
    ' exactly 16 ASCII characters long.
    ' </param>
    ' <param name="keySize">
    ' Size of encryption key in bits. Allowed values are: 128, 192, and 256. 
    ' Longer keys are more secure than shorter keys.
    ' </param>
    ' <returns>
    ' Encrypted value formatted as a base64-encoded string.
    ' </returns>

    ' <summary>
    ' Decrypts specified ciphertext using Rijndael symmetric key algorithm.
    ' </summary>
    ' <param name="cipherText">
    ' Base64-formatted ciphertext value.
    ' </param>
    ' <param name="passPhrase">
    ' Passphrase from which a pseudo-random password will be derived. The 
    ' derived password will be used to generate the encryption key. 
    ' Passphrase can be any string. In this example we assume that this 
    ' passphrase is an ASCII string.
    ' </param>
    ' <param name="saltValue">
    ' Salt value used along with passphrase to generate password. Salt can 
    ' be any string. In this example we assume that salt is an ASCII string.
    ' </param>
    ' <param name="hashAlgorithm">
    ' Hash algorithm used to generate password. Allowed values are: "MD5" and
    ' "SHA1". SHA1 hashes are a bit slower, but more secure than MD5 hashes.
    ' </param>
    ' <param name="passwordIterations">
    ' Number of iterations used to generate password. One or two iterations
    ' should be enough.
    ' </param>
    ' <param name="initVector">
    ' Initialization vector (or IV). This value is required to encrypt the 
    ' first block of plaintext data. For RijndaelManaged class IV must be 
    ' exactly 16 ASCII characters long.
    ' </param>
    ' <param name="keySize">
    ' Size of encryption key in bits. Allowed values are: 128, 192, and 256. 
    ' Longer keys are more secure than shorter keys.
    ' </param>
    ' <returns>
    ' Decrypted string value.
    ' </returns>
    ' <remarks>
    ' Most of the logic in this function is similar to the Encrypt 
    ' logic. In order for decryption to work, all parameters of this function
    ' - except cipherText value - must match the corresponding parameters of 
    ' the Encrypt function which was called to generate the 
    ' ciphertext.
    ' </remarks>
    Public Shared Function Decrypt(ByVal cipherText As String, ByVal saltValue As String) As String

        Dim passPhrase As String = "Int0uchRew@rds"        ' can be any string
        Dim extraSalt As String = ":#938392#"              ' can be any string
        Dim passwordIterations As Integer = 2              ' can be any number
        Dim initVector As String = "@deo329482dsl3Hl"      ' must be 16 bytes
        Dim keySize As Integer = 256                       ' can be 192 or 128

        saltValue = saltValue + extraSalt

        ' Convert strings defining encryption key characteristics into byte
        ' arrays. Let us assume that strings only contain ASCII codes.
        ' If strings include Unicode characters, use Unicode, UTF7, or UTF8
        ' encoding.
        Dim initVectorBytes As Byte()
        initVectorBytes = Encoding.ASCII.GetBytes(initVector)

        Dim saltValueBytes As Byte()
        saltValueBytes = Encoding.ASCII.GetBytes(saltValue)

        ' Convert our ciphertext into a byte array.
        Dim cipherTextBytes As Byte()
        cipherTextBytes = Convert.FromBase64String(cipherText)

        ' First, we must create a password, from which the key will be 
        ' derived. This password will be generated from the specified 
        ' passphrase and salt value. The password will be created using
        ' the specified hash algorithm. Password creation can be done in
        ' several iterations.
        Dim password As Rfc2898DeriveBytes = New Rfc2898DeriveBytes(passPhrase, saltValueBytes, passwordIterations)
        Dim keyBytes As Byte()
        keyBytes = password.GetBytes(keySize / 8)

        ' Create uninitialized Rijndael encryption object.
        Dim symmetricKey As RijndaelManaged
        symmetricKey = New RijndaelManaged()

        ' It is reasonable to set encryption mode to Cipher Block Chaining
        ' (CBC). Use default options for other symmetric key parameters.
        symmetricKey.Mode = CipherMode.CBC

        ' Generate decryptor from the existing key bytes and initialization 
        ' vector. Key size will be defined based on the number of the key 
        ' bytes.
        Dim decryptor As ICryptoTransform
        decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes)

        ' Define memory stream which will be used to hold encrypted data.
        Dim memoryStream As MemoryStream
        memoryStream = New MemoryStream(cipherTextBytes)

        ' Define memory stream which will be used to hold encrypted data.
        Dim cryptoStream As CryptoStream
        cryptoStream = New CryptoStream(memoryStream, _
                                        decryptor, _
                                        CryptoStreamMode.Read)

        ' Since at this point we don't know what the size of decrypted data
        ' will be, allocate the buffer long enough to hold ciphertext;
        ' plaintext is never longer than ciphertext.
        Dim plainTextBytes As Byte()
        ReDim plainTextBytes(cipherTextBytes.Length)

        ' Start decrypting.
        Dim decryptedByteCount As Integer
        decryptedByteCount = cryptoStream.Read(plainTextBytes, _
                                               0, _
                                               plainTextBytes.Length)

        ' Close both streams.
        memoryStream.Close()
        cryptoStream.Close()

        ' Convert decrypted data into a string. 
        ' Let us assume that the original plaintext string was UTF8-encoded.
        Dim plainText As String
        plainText = Encoding.UTF8.GetString(plainTextBytes, _
                                            0, _
                                            decryptedByteCount)

        ' Return decrypted string.
        Decrypt = plainText
    End Function
End Class
